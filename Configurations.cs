namespace AntColonyTSP;

public class Configurations
{
    public int iterations;
    public int antCount;
    public int goal;
    public double startPheromone;
    public int cityCount;
    public double distanceImportance;
    public double pheromoneImportance;
    public double evaporationIntensity;
    public int minDistance;
    public int maxDistance;
    public int threadsCount;

    public Configurations()
    {
        iterations = 100;
        antCount = 50;
        goal = 9000;
        startPheromone = 0.1;
        cityCount = 200;
        distanceImportance = 2;
        pheromoneImportance = 1;
        evaporationIntensity = 0.1;
        minDistance = 100;
        maxDistance = 1000;
        threadsCount = 5;
    }
}