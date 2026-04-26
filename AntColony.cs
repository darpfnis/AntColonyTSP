namespace AntColonyTSP;

internal readonly record struct TourResult(int Distance, int Cost, double Objective, bool Ok);

internal static class AntColony
{
    internal static double ArcWeight(Arc a, Configurations cfg)
    {
        var invD = 1.0 / a.Dist;
        var invC = a.Cost > 0 ? 1.0 / a.Cost : 1.0;
        return Math.Pow(a.Tau, cfg.PheromoneImportance)
               * Math.Pow(invD, cfg.DistanceImportance)
               * Math.Pow(invC, cfg.CostImportance);
    }

    internal static int PickWeightedIndex(Span<double> w, int m, double u)
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

    internal static TourResult SimulateAnt(int n, TspGraph g, Configurations cfg, int[] dirs, int[] path,
        double[] scratch, int[] cand, Random rng)
    {
        for (var c = 0; c < n - 1; c++)
            dirs[c] = c + 1;
        var dirsCount = n - 1;
        path[0] = 0;
        for (var k = 1; k < n; k++)
        {
            var u = path[k - 1];
            var m = 0;
            for (var i = 0; i < dirsCount; i++)
            {
                var v = dirs[i];
                if (!g.TryArc(u, v, out var a))
                    continue;
                cand[m] = i;
                scratch[m] = ArcWeight(a!, cfg);
                m++;
            }
            if (m == 0)
                return new TourResult(0, 0, double.PositiveInfinity, false);
            var pick = PickWeightedIndex(scratch.AsSpan(0, m), m, rng.NextDouble());
            var ix = cand[pick];
            path[k] = dirs[ix];
            dirs[ix] = dirs[--dirsCount];
        }
        path[n] = 0;
        if (!g.TryArc(path[n - 1], 0, out _))
            return new TourResult(0, 0, double.PositiveInfinity, false);
        var span = path.AsSpan(0, n + 1);
        if (!g.TryPathMetrics(span, n + 1, out var dist, out var cost))
            return new TourResult(0, 0, double.PositiveInfinity, false);
        var obj = g.PathObjective(span, n + 1, cfg.PathWeightDistance, cfg.PathWeightCost);
        return new TourResult(dist, cost, obj, true);
    }

    internal static bool MeetsGoal(TourResult r, Configurations cfg) => r.Ok && r.Objective <= cfg.Goal;

    internal static double DepositQ(TourResult r, Configurations cfg) =>
        (double)cfg.Goal / Math.Max(1e-12, r.Objective);
}
