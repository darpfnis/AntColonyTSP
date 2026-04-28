namespace AntColonyTSP;

public class Configurations
{
    public int iterations;
    public int antCount;
    public int goalDistance;
    public int goalCost;
    public double startPheromone;
    public int cityCount;
    public double distanceImportance;
    public double pheromoneImportance;
    public double costImportance;
    public double evaporationIntensity;
    public int minDistance;
    public int maxDistance;
    public int minCost;
    public int maxCost;
    public double noPathProbability;
    public int threadsCount;

    public Configurations()
    {
        iterations = 100;
        antCount = 50;
        goalDistance = 9000;
        goalCost = 9000;
        startPheromone = 0.1;
        cityCount = 200;
        distanceImportance = 2;
        pheromoneImportance = 1;
        costImportance = 2;
        evaporationIntensity = 0.1;
        minDistance = 100;
        maxDistance = 1000;
        minCost = 100;
        maxCost = 1000;
        noPathProbability = 0.1;
        threadsCount = 5;
    }
}
