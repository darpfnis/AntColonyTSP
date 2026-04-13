using AntColonyTSP;

public class Program
{
    public static void Main(string[] args)
    {
        var configurations = new Configurations();
        configurations.goal =  Convert.ToInt32(0.1 * configurations.cityCount * (configurations.minDistance + configurations.maxDistance));
        var adjacencyMatrix = Operators.GetRandomAdjacencyMatrix(configurations.cityCount, configurations.minDistance, configurations.maxDistance);

        // for (var i = 0; i < configurations.cityCount; i++)
        // {
        //     for (var j = 0; j < configurations.cityCount; j++)
        //     {
        //         Console.Write($"{adjacencyMatrix[i, j], 3}" + " ");
        //     }
        //     Console.WriteLine();
        // }
        
        var antColony = new AntColonySequential(adjacencyMatrix, new double[configurations.cityCount,configurations.cityCount],  configurations);
        var result = antColony.Solve();

        for (var i = 0; i < result.path.Count - 1; i++)
        {
            Console.Write(result.path[i] + "(" + adjacencyMatrix[result.path[i], result.path[i + 1]] + ") -> ");
        }
        Console.WriteLine(result.path[^1]);
        
        Console.WriteLine(result.distance);
        Console.WriteLine(configurations.goal);
    }
    
}