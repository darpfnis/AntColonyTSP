using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AntColonyTSP
{
    public class AntColonyTester
    {
        private Configurations _configs;
        private const int RunsPerTest = 3;
        private readonly int[] _citySizes = { 10, 50, 100, 500, 1000 };
        private readonly int[] _threadCounts = { 2, 4, 8 };

        public void RunTests()
        {
            var results = new Dictionary<int, Dictionary<int, double>>();

            Console.WriteLine("Starting comprehensive benchmark according to course work requirements...");
            Console.WriteLine($"Each configuration will be run {RunsPerTest} times to get average values.\n");

            foreach (var size in _citySizes)
            {
                Console.WriteLine($"--- Testing City Size: {size} ---");
                results[size] = new Dictionary<int, double>();

                _configs = new Configurations { cityCount = size };
                _configs.goal = Convert.ToInt32(0.1 * _configs.cityCount * (_configs.minDistance + _configs.maxDistance));
                
                var adjacencyMatrix = Operators.GetRandomAdjacencyMatrix(_configs.cityCount, _configs.minDistance, _configs.maxDistance);

                results[size][1] = RunBenchmark(size, 1, adjacencyMatrix);

                foreach (var threads in _threadCounts)
                {
                    results[size][threads] = RunBenchmark(size, threads, adjacencyMatrix);
                }
                Console.WriteLine();
            }

            PrintTable51(results);
            PrintTable52(results);
        }

        private double RunBenchmark(int citySize, int threads, int[,] matrix)
        {
            string label = threads == 1 ? "Sequential" : $"Parallel ({threads} threads)";
            Console.Write($"  > {label}: ");
            
            _configs.threadsCount = threads;
            List<long> runTimes = new List<long>();

            for (int i = 0; i < RunsPerTest; i++)
            {
                var pheromones = new double[citySize, citySize];
                var sw = Stopwatch.StartNew();
                
                if (threads == 1)
                {
                    var colony = new AntColonySequential(matrix, pheromones, _configs);
                    colony.Solve();
                }
                else
                {
                    var colony = new AntColonyParallel(matrix, pheromones, _configs);
                    colony.Solve();
                }
                
                sw.Stop();
                runTimes.Add(sw.ElapsedMilliseconds);
            }

            double avgMs = runTimes.Average(); 
            Console.WriteLine($"{avgMs:F2} ms");
            return avgMs;
        }

        private void PrintTable51(Dictionary<int, Dictionary<int, double>> results)
        {
            Console.WriteLine("\nTable 5.1. – Time spent by each algorithm (ms)"); 
            string header = string.Format("| {0,-15} | {1,-15} | {2,-15} | {3,-15} | {4,-15} |", 
                "City count", "Sequential", "Parallel (2)", "Parallel (4)", "Parallel (8)");
            Console.WriteLine(new string('-', header.Length));
            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            foreach (var size in _citySizes)
            {
                Console.WriteLine(string.Format("| {0,-15} | {1,-15:F2} | {2,-15:F2} | {3,-15:F2} | {4,-15:F2} |",
                    size, results[size][1], results[size][2], results[size][4], results[size][8])); 
            }
            Console.WriteLine(new string('-', header.Length));
        }

        private void PrintTable52(Dictionary<int, Dictionary<int, double>> results)
        {
            Console.WriteLine("\nTable 5.2 – Time improvement relative to sequential algorithm (Speedup)");
            string header = string.Format("| {0,-15} | {1,-15} | {2,-15} | {3,-15} | {4,-15} |", 
                "City count", "Sequential", "Parallel (2)", "Parallel (4)", "Parallel (8)");
            Console.WriteLine(new string('-', header.Length));
            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            foreach (var size in _citySizes)
            {
                double seq = results[size][1];
                Console.WriteLine(string.Format("| {0,-15} | {1,-15:F1} | {2,-15:F2} | {3,-15:F2} | {4,-15:F2} |",
                    size, 1.0, seq / results[size][2], seq / results[size][4], seq / results[size][8]));
            }
            Console.WriteLine(new string('-', header.Length));
        }
    }
}