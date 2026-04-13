namespace AntColonyTSP;

public static class Operators
{
    public static int GetIndexByProbability(List<double> probabilityDistribution, double probability)
    {
        return probabilityDistribution.FindIndex(p => probability <= p);
    }
    
    public static double GetRandomDouble(double lower, double upper)
    {
        return lower + Random.Shared.NextDouble() * (upper - lower);
    }

    public static int[,] GetRandomAdjacencyMatrix(int size, int min, int max)
    {
        var adjacencyMatrix = new int[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = i; j < size; j++)
            {
                if (i == j)
                {
                    adjacencyMatrix[i, j] = 0;    
                    continue;
                }
                
                adjacencyMatrix[i, j] = Random.Shared.Next(min, max);
                adjacencyMatrix[j, i] = adjacencyMatrix[i, j];
            }
        }

        return adjacencyMatrix;
    }
}