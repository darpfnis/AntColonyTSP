namespace AntColonyTSP;

public class AntColony
{
    protected readonly Path?[,] _adjacencyMatrix;
    protected Configurations Config;

    protected AntColony(Path?[,] adjacencyMatrix, Configurations configurations)
    {
        _adjacencyMatrix = adjacencyMatrix;
        Config = configurations;
    }
    
    protected (int distance, int cost) EvaluateAntPath(List<int> path)
    {
        var distance = 0;
        var cost = 0;
        for (var i = 0; i < path.Count - 1; i++)
        {
            if (_adjacencyMatrix[path[i], path[i + 1]] == null)
            {
                continue;
            }
            distance += _adjacencyMatrix[path[i], path[i + 1]].distance;
            cost += _adjacencyMatrix[path[i], path[i + 1]].cost;
        }
        return (distance, cost);
    }
    
    protected List<double> CalculateProbabilityDistribution(int startingPoint, List<int> availableDirections, Configurations configurations)
    {
        var directionProbability = availableDirections
            .Select(direction => 
                Math.Pow(_adjacencyMatrix[startingPoint, direction].pheromone, configurations.pheromoneImportance) *
                Math.Pow(1.0 / _adjacencyMatrix[startingPoint, direction].distance, configurations.distanceImportance) *
                Math.Pow(1.0 / _adjacencyMatrix[startingPoint, direction].cost, configurations.costImportance))
            .ToList();

        var totalProbability = directionProbability.Sum();
        directionProbability = directionProbability.Select(p => p / totalProbability).ToList();
        
        double sum = 0;
        var probabilityDistribution = directionProbability.Select(p => sum += p).ToList();
        probabilityDistribution[^1] = 1.0;
        
        return probabilityDistribution;
    }

    protected void ApplyToMatrix(double operand, Func<double, double, double> operation)
    {
        var size = _adjacencyMatrix.GetLength(0);
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                if (_adjacencyMatrix[i, j] == null)
                {
                    continue;
                }
                _adjacencyMatrix[i, j].pheromone = operation(_adjacencyMatrix[i, j].pheromone, operand);
            }
        }
    }

    protected void ApplyPheromoneFromPath(AntPath antPath)
    {
        for (var i = 0; i < antPath.path.Count - 1; i++)
        {
            _adjacencyMatrix[antPath.path[i], antPath.path[i + 1]].pheromone += Config.goalDistance * 1.0 / antPath.distance + Config.goalCost * 1.0 / antPath.cost;
        }
    }
}
