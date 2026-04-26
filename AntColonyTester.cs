using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace AntColonyTSP;

public sealed class AntColonyTester
{
    const int RunsPerConfig = 7;
    const int W = 15;
    static readonly int[] ThreadCounts = { 1, 2, 4, 6, 8, 12 };
    static readonly int[] TableParallelP = { 2, 4, 6, 8, 12 };
    static readonly int[] CitySizes = { 50, 100, 500, 1000, 3000};
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public void RunTests()
    {
        Console.WriteLine("Benchmark: directed ATSP");
        var timesByN = new Dictionary<int, Dictionary<int, double>>();
        foreach (var n in CitySizes)
        {
            var cfg = new Configurations { CityCount = n };
            cfg.PathWeightDistance = 1;
            cfg.PathWeightCost = 0.15;
            cfg.CostImportance = 1;
            cfg.Goal = (int)(0.1 * n * (cfg.MinDistance + cfg.MaxDistance) * cfg.PathWeightDistance
                + 0.1 * n * (cfg.MinCost + cfg.MaxCost) * cfg.PathWeightCost);
            var master = TspGraph.CreateDirectedComplete(Random.Shared, n, cfg.MinDistance, cfg.MaxDistance, cfg.MinCost, cfg.MaxCost, cfg.StartPheromone);
            Console.WriteLine("--- Testing City Size: {0} ---", n);
            var times = new Dictionary<int, double>();
            foreach (var p in ThreadCounts)
            {
                var ms = MedianTimeMs(cfg, master, p);
                times[p] = ms;
                if (p == 1)
                    Console.WriteLine(string.Format(Inv, "  > Sequential: {0:F2} ms", ms));
                else
                    Console.WriteLine(string.Format(Inv, "  > Parallel ({0} threads): {1:F2} ms", p, ms));
            }
            timesByN[n] = times;
            Console.WriteLine();
        }
        Table51Time(timesByN);
        Table52Speedup(timesByN);
        RunAmdahlProbe();
        Console.WriteLine();
        var mid = new Configurations { CityCount = 80 };
        mid.PathWeightDistance = 1;
        mid.PathWeightCost = 0.15;
        mid.CostImportance = 1;
        mid.Goal = (int)(0.1 * 80 * (mid.MinDistance + mid.MaxDistance) * mid.PathWeightDistance
            + 0.1 * 80 * (mid.MinCost + mid.MaxCost) * mid.PathWeightCost);
        var g80 = TspGraph.CreateDirectedComplete(Random.Shared, 80, mid.MinDistance, mid.MaxDistance, mid.MinCost, mid.MaxCost, mid.StartPheromone);
        RunSensitivityAntCount(mid, g80);
        RunSensitivityEvaporation(mid, g80);
    }

    static double MedianTimeMs(Configurations template, TspGraph master, int threads)
    {
        var xs = new double[RunsPerConfig];
        for (var r = 0; r < RunsPerConfig; r++)
        {
            var g = master.CloneTau();
            var cfg = CloneCfg(template);
            cfg.ThreadsCount = threads;
            var sw = Stopwatch.StartNew();
            if (threads == 1)
                new AntColonySequential(g, cfg).Solve();
            else
                new AntColonyParallel(g, cfg).Solve();
            sw.Stop();
            xs[r] = sw.Elapsed.TotalMilliseconds;
        }
        Array.Sort(xs);
        return xs[xs.Length / 2];
    }

    static void RunAmdahlProbe()
    {
        var cfg = new Configurations { CityCount = 100, AntCount = 96, Iterations = 25, ThreadsCount = 12 };
        cfg.PathWeightDistance = 1;
        cfg.PathWeightCost = 0.15;
        cfg.CostImportance = 1;
        cfg.Goal = (int)(0.1 * 100 * (cfg.MinDistance + cfg.MaxDistance) * cfg.PathWeightDistance
            + 0.1 * 100 * (cfg.MinCost + cfg.MaxCost) * cfg.PathWeightCost);
        var g = TspGraph.CreateDirectedComplete(Random.Shared, 100, cfg.MinDistance, cfg.MaxDistance, cfg.MinCost, cfg.MaxCost, cfg.StartPheromone);
        new AntColonyParallel(g, cfg).Solve();
        var f = AntColonyParallel.EstimatedSerialFraction;
        Console.WriteLine("Amdahl (code-based proxy per iteration):");
        Console.WriteLine(string.Format(Inv,
            "  T_ant ≈ {0:F2} ms, T_merge ≈ {1:F2} ms, f_lower = T_merge / (T_ant + T_merge) ≈ {2:F3}.",
            AntColonyParallel.LastAvgAntPhaseMs, AntColonyParallel.LastAvgMergeMs, f));
    }

    static void Table51Time(Dictionary<int, Dictionary<int, double>> t)
    {
        var head = new[] { "City count", "Sequential", "Parallel (2)", "Parallel (4)", "Parallel (6)", "Parallel (8)", "Parallel (12)" };
        var hdr = RowText(head);
        var rule = new string('-', hdr.Length);
        Console.WriteLine(rule);
        Console.WriteLine(hdr);
        Console.WriteLine(rule);
        foreach (var n in CitySizes)
        {
            var c = t[n];
            var cells = new object[2 + TableParallelP.Length];
            cells[0] = n;
            cells[1] = c[1];
            for (var i = 0; i < TableParallelP.Length; i++)
                cells[2 + i] = c[TableParallelP[i]];
            Console.WriteLine(RowFloat(cells));
        }
        Console.WriteLine(rule);
        Console.WriteLine();
    }

    static void Table52Speedup(Dictionary<int, Dictionary<int, double>> t)
    {
        var head = new[] { "City count", "Sequential", "Parallel (2)", "Parallel (4)", "Parallel (6)", "Parallel (8)", "Parallel (12)" };
        var hdr = RowText(head);
        var rule = new string('-', hdr.Length);
        Console.WriteLine(rule);
        Console.WriteLine(hdr);
        Console.WriteLine(rule);
        foreach (var n in CitySizes)
        {
            var c = t[n];
            var t1 = c[1];
            var cells = new object[2 + TableParallelP.Length];
            cells[0] = n;
            cells[1] = 1.0;
            for (var i = 0; i < TableParallelP.Length; i++)
                cells[2 + i] = t1 / c[TableParallelP[i]];
            Console.WriteLine(RowFloat(cells));
        }
        Console.WriteLine(rule);
        Console.WriteLine();
    }
    
    

    static string RowText(string[] cells)
    {
        var s = new StringBuilder();
        s.Append("| ");
        s.Append(cells[0].PadRight(W));
        for (var i = 1; i < cells.Length; i++)
        {
            s.Append(" | ");
            s.Append(cells[i].PadRight(W));
        }
        s.Append(" |");
        return s.ToString();
    }

    static string RowFloat(object[] cells)
    {
        var s = new StringBuilder();
        s.Append("| ");
        s.Append(cells[0].ToString()!.PadRight(W));
        for (var i = 1; i < cells.Length; i++)
        {
            s.Append(" | ");
            s.AppendFormat(Inv, "{0,15:F2}", cells[i]);
        }
        s.Append(" |");
        return s.ToString();
    }

    static void RunSensitivityAntCount(Configurations baseCfg, TspGraph master80)
    {
        Console.WriteLine("Sensitivity: AntCount, parallel 12 threads, n=80.");
        Console.WriteLine("| AntCount | Time (ms) | Best F (last run) |");
        Console.WriteLine("|----------|-----------|-------------------|");
        foreach (var m in new[] { 16, 32, 64, 128, 256 })
        {
            var cfg = CloneCfg(baseCfg);
            cfg.AntCount = m;
            cfg.ThreadsCount = 12;
            var g = master80.CloneTau();
            var sw = Stopwatch.StartNew();
            var path = new AntColonyParallel(g, cfg).Solve();
            sw.Stop();
            Console.WriteLine(string.Format(Inv, "| {0,8} | {1,9:F2} | {2,17:F2} |", m, sw.Elapsed.TotalMilliseconds, path.Objective));
        }
        Console.WriteLine();
    }

    static void RunSensitivityEvaporation(Configurations baseCfg, TspGraph master80)
    {
        Console.WriteLine("Sensitivity: EvaporationIntensity, sequential, n=80.");
        Console.WriteLine("| Evaporation | Time (ms) | Best F (last run) |");
        Console.WriteLine("|-------------|-----------|-------------------|");
        foreach (var rho in new[] { 0.05, 0.1, 0.2, 0.3, 0.5 })
        {
            var cfg = CloneCfg(baseCfg);
            cfg.EvaporationIntensity = rho;
            cfg.ThreadsCount = 1;
            var g = master80.CloneTau();
            var sw = Stopwatch.StartNew();
            var path = new AntColonySequential(g, cfg).Solve();
            sw.Stop();
            Console.WriteLine(string.Format(Inv, "| {0,11:F2} | {1,9:F2} | {2,17:F2} |", rho, sw.Elapsed.TotalMilliseconds, path.Objective));
        }
    }

    static Configurations CloneCfg(Configurations c) => new()
    {
        Iterations = c.Iterations,
        AntCount = c.AntCount,
        Goal = c.Goal,
        StartPheromone = c.StartPheromone,
        CityCount = c.CityCount,
        DistanceImportance = c.DistanceImportance,
        PheromoneImportance = c.PheromoneImportance,
        CostImportance = c.CostImportance,
        PathWeightDistance = c.PathWeightDistance,
        PathWeightCost = c.PathWeightCost,
        EvaporationIntensity = c.EvaporationIntensity,
        MinDistance = c.MinDistance,
        MaxDistance = c.MaxDistance,
        MinCost = c.MinCost,
        MaxCost = c.MaxCost,
        ThreadsCount = c.ThreadsCount
    };
}
