namespace AntColonyTSP;

public class AntColonyParallel : AntColony
{
    private static readonly ThreadLocal<Random> _threadRandom =
        new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

    public AntColonyParallel(Path?[,] adjacencyMatrix, Configurations configurations)
        : base(adjacencyMatrix, configurations)
    {
    }

    public AntPath Solve()
    {
        var size = Config.cityCount;
        var lockObject = new object();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.threadsCount };

        AntPath? globalBestPath = null;
        ApplyToMatrix(Config.startPheromone, (_, value) => value);

        for (var i = 0; i < Config.iterations; i++)
        {
            var localPaths = new AntPath[Config.antCount];
            for (var k = 0; k < Config.antCount; k++)
                localPaths[k] = new AntPath();

            var foundGoal = false;

            Parallel.For(0, Config.antCount, parallelOptions, j =>
            {
                var rng = _threadRandom.Value!;
                var currentCity = 0;
                var unvisited = Enumerable.Range(1, size - 1).ToList();
                var path = new List<int> { currentCity };

                while (unvisited.Count > 0)
                {
                    var reachableDirections = unvisited
                        .Where(targetCity => _adjacencyMatrix[currentCity, targetCity] != null)
                        .ToList();

                    if (reachableDirections.Count == 0)
                    {
                        path.Clear();
                        break;
                    }

                    var probabilityDistribution =
                        CalculateProbabilityDistribution(currentCity, reachableDirections, Config);
                    var probability = rng.NextDouble();
                    var index = Operators.GetIndexByProbability(probabilityDistribution, probability);

                    var nextCity = reachableDirections[index];
                    path.Add(nextCity);
                    unvisited.Remove(nextCity);
                    currentCity = nextCity;
                }

                if (path.Count > 0 && _adjacencyMatrix[currentCity, 0] != null)
                {
                    path.Add(0);
                    var (dist, cost) = EvaluateAntPath(path);

                    localPaths[j].path = path;
                    localPaths[j].distance = dist;
                    localPaths[j].cost = cost;
                    localPaths[j].objectiveValue =
                        (Config.distanceImportance * dist) + (Config.costImportance * cost);
                }
                else
                {
                    localPaths[j].distance = int.MaxValue;
                }
            });

            foreach (var path in localPaths)
            {
                if (path.distance == int.MaxValue) continue;

                if (path.distance <= Config.goalDistance && path.cost <= Config.goalCost)
                    return path;

                if (globalBestPath == null || path.objectiveValue < globalBestPath.objectiveValue)
                    globalBestPath = path;
            }

            ApplyToMatrix(1 - Config.evaporationIntensity, (current, value) => current * value);

            foreach (var path in localPaths.Where(p => p.distance < int.MaxValue))
            {
                ApplyPheromoneFromPath(path);
            }
        }

        return globalBestPath ?? new AntPath();
    }
}
