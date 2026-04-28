using System.Globalization;

namespace AntColonyTSP;

public sealed class Arc
{
    public int Id;
    public int From;
    public int To;
    public int Dist;
    public int Cost;
    public double Tau;
}

public sealed class TspGraph
{
    public int N { get; }
    readonly Arc[] _byId;
    readonly Arc?[][] _matrix;

    TspGraph(int n, Arc[] byId, Arc?[][] matrix)
    {
        N = n;
        _byId = byId;
        _matrix = matrix;
    }

    public bool TryArc(int from, int to, out Arc? a)
    {
        a = _matrix[from][to];
        return a != null;
    }

    public Arc Arc(int from, int to) => _matrix[from][to]!;

    public static TspGraph CreateDirectedComplete(Random rng, int n, int minD, int maxD, int minC, int maxC, double tau0)
    {
        var eCount = n * (n - 1);
        var byId = new Arc[eCount];
        var matrix = new Arc?[n][];
        
        for (var i = 0; i < n; i++)
            matrix[i] = new Arc?[n];

        var id = 0;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                if (i == j)
                    continue;
                
                var a = new Arc
                {
                    Id = id++,
                    From = i,
                    To = j,
                    Dist = rng.Next(minD, maxD),
                    Cost = rng.Next(minC, maxC),
                    Tau = tau0
                };
                byId[a.Id] = a;
                matrix[i][j] = a;
            }
        }
        return new TspGraph(n, byId, matrix);
    }

    public TspGraph CloneTau()
    {
        var byId = new Arc[_byId.Length];
        var matrix = new Arc?[N][];
        
        for (var i = 0; i < N; i++)
            matrix[i] = new Arc?[N];

        for (var k = 0; k < _byId.Length; k++)
        {
            var o = _byId[k];
            var a = new Arc 
            { 
                Id = o.Id, 
                From = o.From, 
                To = o.To, 
                Dist = o.Dist, 
                Cost = o.Cost, 
                Tau = o.Tau 
            };
            byId[k] = a;
            matrix[a.From][a.To] = a;
        }
        return new TspGraph(N, byId, matrix);
    }

    public void ResetTau(double tau0)
    {
        foreach (var e in _byId)
            e.Tau = tau0;
    }

    public void EvaporateAndAdd(double evap, ReadOnlySpan<double> deltaSumById)
    {
        for (var k = 0; k < _byId.Length; k++)
            _byId[k].Tau = _byId[k].Tau * evap + deltaSumById[k];
    }

    public void EvaporateAndAdd(double evap, IReadOnlyList<double[]> threadDeltas)
    {
        for (var k = 0; k < _byId.Length; k++)
        {
            var v = _byId[k].Tau * evap;
            foreach (var d in threadDeltas)
                v += d[k];
            _byId[k].Tau = v;
        }
    }

    public int EdgeCount => _byId.Length;

    public void AddPathDelta(ReadOnlySpan<int> path, int pathLen, double q, double[] delta)
    {
        for (var i = 0; i < pathLen - 1; i++)
            delta[Arc(path[i], path[i + 1]).Id] += q;
    }

    public bool TryPathMetrics(ReadOnlySpan<int> path, int len, out int dist, out int cost)
    {
        dist = 0;
        cost = 0;
        for (var i = 0; i < len - 1; i++)
        {
            var a = _matrix[path[i]][path[i + 1]];
            if (a is null)
                return false;
            dist += a.Dist;
            cost += a.Cost;
        }
        return true;
    }

    public double PathObjective(ReadOnlySpan<int> path, int len, double wd, double wc)
    {
        if (!TryPathMetrics(path, len, out var d, out var c))
            return double.PositiveInfinity;
        if (wd <= 0 && wc <= 0)
            return d;
        if (wd <= 0)
            return c;
        if (wc <= 0)
            return d;
        return wd * d + wc * c;
    }

    public void DumpTo(TextWriter w)
    {
        var inv = CultureInfo.InvariantCulture;
        w.WriteLine(string.Format(inv, "n={0}, |E|={1} (directed arcs From->To)", N, EdgeCount));
        w.WriteLine("| Id | From | To | Dist | Cost | Tau   |");
        w.WriteLine("|----|------|----|------|------|-------|");
        foreach (var e in _byId)
        {
            w.WriteLine(string.Format(inv, "| {0,2} | {1,4} | {2,2} | {3,4} | {4,4} | {5,5:F3} |", 
                e.Id, e.From, e.To, e.Dist, e.Cost, e.Tau));
        }
    }

    public static void PrintSampleExample(TextWriter? w = null)
    {
        w ??= Console.Out;
        w.WriteLine("Example directed graph n=3 (seed=42): arc (i->j) has its own Dist/Cost; (0->1) != (1->0).");
        var g = CreateDirectedComplete(new Random(42), 3, 10, 50, 5, 20, 0.1);
        g.DumpTo(w);
    }
}
