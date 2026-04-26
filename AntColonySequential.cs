using System.Buffers;

namespace AntColonyTSP;

public sealed class AntColonySequential
{
    readonly Configurations _cfg;
    readonly TspGraph _g;
    readonly double[] _deposit;

    public AntColonySequential(TspGraph graph, Configurations cfg)
    {
        _g = graph;
        _cfg = cfg;
        _deposit = new double[graph.EdgeCount];
    }

    public AntPath Solve()
    {
        var n = _cfg.CityCount;
        var poolI = ArrayPool<int>.Shared;
        var poolD = ArrayPool<double>.Shared;
        var dirs = poolI.Rent(n);
        var path = poolI.Rent(n + 1);
        var cand = poolI.Rent(n);
        var scratch = poolD.Rent(n);
        var best = new AntPath { Distance = int.MaxValue, Objective = double.MaxValue, Nodes = new int[n + 1] };
        try
        {
            _g.ResetTau(_cfg.StartPheromone);
            for (var it = 0; it < _cfg.Iterations; it++)
            {
                Array.Clear(_deposit);
                for (var a = 0; a < _cfg.AntCount; a++)
                {
                    var r = AntColony.SimulateAnt(n, _g, _cfg, dirs, path, scratch, cand, Random.Shared);
                    if (!r.Ok)
                        continue;
                    var q = AntColony.DepositQ(r, _cfg);
                    _g.AddPathDelta(path.AsSpan(0, n + 1), n + 1, q, _deposit);
                    if (AntColony.MeetsGoal(r, _cfg))
                    {
                        var nodes = new int[n + 1];
                        path.AsSpan(0, n + 1).CopyTo(nodes);
                        return new AntPath { Nodes = nodes, Distance = r.Distance, Cost = r.Cost, Objective = r.Objective };
                    }
                    if (r.Ok && r.Objective < best.Objective)
                    {
                        best.Distance = r.Distance;
                        best.Cost = r.Cost;
                        best.Objective = r.Objective;
                        path.AsSpan(0, n + 1).CopyTo(best.Nodes);
                    }
                }
                _g.EvaporateAndAdd(1.0 - _cfg.EvaporationIntensity, _deposit);
            }
            return best;
        }
        finally
        {
            poolI.Return(dirs);
            poolI.Return(path);
            poolI.Return(cand);
            poolD.Return(scratch);
        }
    }
}
