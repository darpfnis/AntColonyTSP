namespace AntColonyTSP;

public class AntColonySequential : AntColony
{
    public AntColonySequential(Path?[,] adjacencyMatrix, Configurations configurations)
        : base(adjacencyMatrix, configurations)
    {
    }

    public AntPath Solve()
    {
        var paths = Enumerable.Range(0, Config.antCount).Select(_ => new AntPath()).ToList();
        var bestPathIndex = 0;
        ApplyToMatrix(Config.startPheromone, (current, value) => value);

        for (var i = 0; i < Config.iterations; i++)
        {
            for (var j = 0; j < Config.antCount; j++)
            {
                var currentCity = 0;
                var unvisited = Enumerable.Range(1, Config.cityCount - 1).ToList();
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
                    var probability = Random.Shared.NextDouble();
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
    
                    paths[j].path = path;
                    paths[j].distance = dist;
                    paths[j].cost = cost;
    
                    paths[j].objectiveValue = (Config.distanceImportance * dist) + (Config.costImportance * cost);

                    if (dist <= Config.goalDistance && cost <= Config.goalCost)
                    {
                        return paths[j];
                    }

                    if (paths[bestPathIndex].distance == int.MaxValue || 
                        paths[j].objectiveValue < paths[bestPathIndex].objectiveValue)
                    {
                        bestPathIndex = j;
                    }
                }
                else
                {
                    paths[j].distance = int.MaxValue;
                }
            }

            ApplyToMatrix(1 - Config.evaporationIntensity, (current, value) => current * value);
            foreach (var path in paths.Where(p => p.distance < int.MaxValue))
            {
                ApplyPheromoneFromPath(path);
            }
        }

        return paths[bestPathIndex];
    }
}
