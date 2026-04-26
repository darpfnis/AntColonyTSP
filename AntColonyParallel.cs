using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AntColonyTSP;

public sealed class AntColonyParallel
{
    public static double LastAvgAntPhaseMs { get; private set; }
    public static double LastAvgMergeMs { get; private set; }

    public static double EstimatedSerialFraction =>
        LastAvgMergeMs / Math.Max(1e-12, LastAvgAntPhaseMs + LastAvgMergeMs);

    readonly Configurations _cfg;
    readonly TspGraph _g;

    public AntColonyParallel(TspGraph graph, Configurations cfg)
    {
        _g = graph;
        _cfg = cfg;
    }

    public AntPath Solve()
    {
        var n = _cfg.CityCount;
        var eCount = _g.EdgeCount;
        var po = new ParallelOptions { MaxDegreeOfParallelism = _cfg.ThreadsCount };
        var best = new AntPath { Distance = int.MaxValue, Cost = int.MaxValue, Objective = double.MaxValue, Nodes = new int[n + 1] };
        var bestLock = new object();
        
        // ThreadLocal гарантує унікальний буфер на потік
        var deltaBag = new ConcurrentBag<double[]>();
        using var tls = new ThreadLocal<double[]>(() =>
        {
            var buf = new double[eCount];
            deltaBag.Add(buf);
            return buf;
        });
        
        _g.ResetTau(_cfg.StartPheromone);
        double sumAnt = 0, sumMerge = 0;
        
        for (var it = 0; it < _cfg.Iterations; it++)
        {
            foreach (var b in deltaBag)
                Array.Clear(b);
            
            var goalFound = 0;
            var swAnt = Stopwatch.StartNew();
            
            Parallel.ForEach(Partitioner.Create(0, _cfg.AntCount), po,
                () => (
                    localBest: new AntPath { Distance = int.MaxValue, Cost = int.MaxValue, Objective = double.MaxValue, Nodes = new int[n + 1] },
                    dirs: ArrayPool<int>.Shared.Rent(n),
                    path: ArrayPool<int>.Shared.Rent(n + 1),
                    cand: ArrayPool<int>.Shared.Rent(n),
                    scratch: ArrayPool<double>.Shared.Rent(n),
                    rng: new Random(Guid.NewGuid().GetHashCode())
                ),
                (range, state, local) =>
                {
                    if (Interlocked.CompareExchange(ref goalFound, 0, 0) != 0)
                    {
                        state.Stop();
                        return local;
                    }
                    
                    var delta = tls.Value!;
                    
                    for (var ant = range.Item1; ant < range.Item2; ant++)
                    {
                        if (Interlocked.CompareExchange(ref goalFound, 0, 0) != 0)
                        {
                            state.Stop();
                            break;
                        }
                        
                        var r = SimulateAntOptimized(n, local.dirs, local.path, local.scratch, local.cand, local.rng);
                        
                        if (!r.Ok)
                            continue;
                        
                        if (AntColony.MeetsGoal(r, _cfg))
                        {
                            Interlocked.Exchange(ref goalFound, 1);
                            local.localBest.Distance = r.Distance;
                            local.localBest.Cost = r.Cost;
                            local.localBest.Objective = r.Objective;
                            local.path.AsSpan(0, n + 1).CopyTo(local.localBest.Nodes);
                            state.Stop();
                            break;
                        }
                        
                        var q = AntColony.DepositQ(r, _cfg);
                        _g.AddPathDelta(local.path.AsSpan(0, n + 1), n + 1, q, delta);
                        
                        if (r.Objective < local.localBest.Objective)
                        {
                            local.localBest.Distance = r.Distance;
                            local.localBest.Cost = r.Cost;
                            local.localBest.Objective = r.Objective;
                            local.path.AsSpan(0, n + 1).CopyTo(local.localBest.Nodes);
                        }
                    }
                    return local;
                },
                local =>
                {
                    ArrayPool<int>.Shared.Return(local.dirs);
                    ArrayPool<int>.Shared.Return(local.path);
                    ArrayPool<int>.Shared.Return(local.cand);
                    ArrayPool<double>.Shared.Return(local.scratch);
                    
                    lock (bestLock)
                    {
                        if (local.localBest.Objective < best.Objective)
                        {
                            best.Distance = local.localBest.Distance;
                            best.Cost = local.localBest.Cost;
                            best.Objective = local.localBest.Objective;
                            Array.Copy(local.localBest.Nodes, best.Nodes, n + 1);
                        }
                    }
                });
            
            swAnt.Stop();
            sumAnt += swAnt.Elapsed.TotalMilliseconds;
            
            if (Interlocked.CompareExchange(ref goalFound, 0, 0) != 0)
            {
                LastAvgAntPhaseMs = sumAnt / (it + 1);
                LastAvgMergeMs = sumMerge / (it + 1);
                return best;
            }
            
            var swM = Stopwatch.StartNew();
            var deltas = deltaBag.ToList();
            _g.EvaporateAndAdd(1.0 - _cfg.EvaporationIntensity, deltas);
            swM.Stop();
            sumMerge += swM.Elapsed.TotalMilliseconds;
        }
        
        LastAvgAntPhaseMs = sumAnt / _cfg.Iterations;
        LastAvgMergeMs = sumMerge / _cfg.Iterations;
        return best;
    }

    // Оптимізована версія SimulateAnt для повного графа
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    TourResult SimulateAntOptimized(int n, int[] dirs, int[] path, double[] scratch, int[] cand, Random rng)
    {
        // Ініціалізація доступних міст
        for (var c = 0; c < n - 1; c++)
            dirs[c] = c + 1;
        var dirsCount = n - 1;
        
        path[0] = 0;
        
        for (var k = 1; k < n; k++)
        {
            var u = path[k - 1];
            var m = 0;
            
            // Для повного графа дуга завжди існує - без TryArc!
            for (var i = 0; i < dirsCount; i++)
            {
                var v = dirs[i];
                var a = _g.Arc(u, v); // Прямий доступ без перевірки
                cand[m] = i;
                scratch[m] = ArcWeightInlined(a);
                m++;
            }
            
            var pick = PickWeightedIndexInlined(scratch, m, rng.NextDouble());
            var ix = cand[pick];
            path[k] = dirs[ix];
            dirs[ix] = dirs[--dirsCount];
        }
        
        path[n] = 0;
        
        var span = path.AsSpan(0, n + 1);
        if (!_g.TryPathMetrics(span, n + 1, out var dist, out var cost))
            return new TourResult(0, 0, double.PositiveInfinity, false);
        
        var obj = _g.PathObjective(span, n + 1, _cfg.PathWeightDistance, _cfg.PathWeightCost);
        return new TourResult(dist, cost, obj, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    double ArcWeightInlined(Arc a)
    {
        var invD = 1.0 / a.Dist;
        var invC = a.Cost > 0 ? 1.0 / a.Cost : 1.0;
        return Math.Pow(a.Tau, _cfg.PheromoneImportance)
               * Math.Pow(invD, _cfg.DistanceImportance)
               * Math.Pow(invC, _cfg.CostImportance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int PickWeightedIndexInlined(double[] w, int m, double u)
    {
        double tot = 0;
        for (var i = 0; i < m; i++)
            tot += w[i];
        
        if (tot <= 0)
            return Random.Shared.Next(m);
        
        var inv = 1.0 / tot;
        double acc = 0;
        var last = m - 1;
        
        for (var i = 0; i < last; i++)
        {
            acc += w[i] * inv;
            if (u < acc)
                return i;
        }
        
        return last; 
    }
}