namespace AntColonyTSP;

public class AntColonyParallel : AntColony
{
    public AntColonyParallel(int[,] adjacencyMatrix, double[,] pheromoneMatrix, Configurations configurations)
        : base(adjacencyMatrix, pheromoneMatrix, configurations)
    {
    }

    public AntPath Solve()
    {
        var paths = Enumerable.Range(0, Config.antCount).Select(_ => new AntPath()).ToList();
        AntPath? globalBestPath = null;
        object lockObject = new object();

        ApplyToMatrix(Config.startPheromone, (current, value) => value);

        for (var i = 0; i < Config.iterations; i++)
        {
            var parallelOptions = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Config.threadsCount 
            };

            // Паралельний цикл по мурахах
            Parallel.For(0, Config.antCount, parallelOptions, j =>
            {
                var availableDirections = Enumerable.Range(1, Config.cityCount - 1).ToList();
                var currentPath = Enumerable.Repeat(0, Config.cityCount + 1).ToList();

                for (var k = 1; k < Config.cityCount; k++)
                {
                    var probabilityDistribution = CalculateProbabilityDistribution(currentPath[k - 1], availableDirections, Config);
                    var probability = Operators.GetRandomDouble(0, 1);
                    var index = Operators.GetIndexByProbability(probabilityDistribution, probability);
                    
                    currentPath[k] = availableDirections[index];
                    availableDirections.RemoveAt(index);
                }

                paths[j].path = currentPath;
                paths[j].distance = EvaluateAntPath(currentPath);

                // Синхронізація для оновлення найкращого шляху ітерації
                lock (lockObject)
                {
                    if (globalBestPath == null || paths[j].distance < globalBestPath.distance)
                    {
                        globalBestPath = paths[j];
                    }
                }
            });

            if (globalBestPath != null && globalBestPath.distance <= Config.goal)
            {
                return globalBestPath;
            }

            ApplyToMatrix(1 - Config.evaporationIntensity, (current, value) => current * value);

            foreach (var path in paths)
            {
                ApplyPheromoneFromPath(path);
            }

            Console.WriteLine($"Iteration {i} best result: {globalBestPath?.distance}");
        }

        return globalBestPath ?? paths[0];
    }
} 