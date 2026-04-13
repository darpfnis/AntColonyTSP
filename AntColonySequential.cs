using System.Numerics;
using AntColonyTSP;

namespace AntColonyTSP;

public class AntColonySequential : AntColony
{
    public AntColonySequential(int[,] adjacencyMatrix, double[,] pheromoneMatrix, Configurations configurations)
        : base(adjacencyMatrix, pheromoneMatrix, configurations)
    {
    }
    
    public AntPath Solve()
    {
        var paths = Enumerable.Range(0, Config.antCount).Select(_ => new AntPath()).ToList();
        var bestPath = 0;
        ApplyToMatrix(Config.startPheromone, (current, value) => value);
        
        for (var i = 0; i < Config.iterations; i++)
        {
            bestPath = 0;
            for (var j = 0; j < Config.antCount; j++)
            {
                var availableDirections = Enumerable.Range(1, Config.cityCount - 1).ToList();
                var path = Enumerable.Repeat(0, Config.cityCount + 1).ToList();
                
                for (var k = 1; k < Config.cityCount; k++)
                {
                    var probabilityDistribution = CalculateProbabilityDistribution(path[k - 1], availableDirections, Config);
                    var probability = Operators.GetRandomDouble(0, 1);
                    var index = Operators.GetIndexByProbability(probabilityDistribution, probability);
                    path[k] = availableDirections[index];
                    availableDirections.RemoveAt(index);
                }
                
                paths[j].path = path;
                paths[j].distance = EvaluateAntPath(path);

                if (paths[j].distance <= Config.goal)
                {
                    return paths[j];
                }

                if (paths[j].distance < paths[bestPath].distance)
                {
                    bestPath = j;
                }
            }

            ApplyToMatrix(1 - Config.evaporationIntensity, (current, value) => current * value);

            foreach (var path in paths)
            {
                ApplyPheromoneFromPath(path);
            }

            Console.WriteLine("Iteration " + i + " best result: " + paths[bestPath].distance);
        }
        
        return paths[bestPath];
    }
}