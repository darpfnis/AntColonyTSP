namespace AntColonyTSP;

public class AntColony
{
    private readonly int[,] _adjacencyMatrix;
    protected double[,] _pheromoneMatrix;
    protected Configurations Config;

    protected AntColony(int[,] adjacencyMatrix, double[,] pheromoneMatrix, Configurations configurations)
    {
        _adjacencyMatrix = adjacencyMatrix;
        _pheromoneMatrix = pheromoneMatrix;
        Config = configurations;
    }
    
    protected int EvaluateAntPath(List<int> path)
    {
        var distance = 0;
        for (var i = 0; i < path.Count - 1; i++)
        {
            distance += _adjacencyMatrix[path[i], path[i + 1]];
        }
        return distance;
    }
    
    protected List<double> CalculateProbabilityDistribution(int startingPoint, List<int> availableDirections, Configurations configurations)
    {
        var directionProbability = availableDirections
            .Select(direction => 
                Math.Pow(_pheromoneMatrix[startingPoint, direction], configurations.pheromoneImportance) *
                Math.Pow(1.0 / _adjacencyMatrix[startingPoint, direction], configurations.distanceImportance))
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
        var size = _pheromoneMatrix.GetLength(0);
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                if (i == j) continue;
                _pheromoneMatrix[i, j] = operation(_pheromoneMatrix[i, j], operand);
            }
        }
    }

    protected void ApplyPheromoneFromPath(AntPath antPath)
    {
        for (var i = 0; i < antPath.path.Count - 1; i++)
        {
            _pheromoneMatrix[antPath.path[i], antPath.path[i + 1]] += Config.goal * 1.0 / antPath.distance;
        }
    }
}