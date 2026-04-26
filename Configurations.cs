namespace AntColonyTSP;

public sealed class Configurations
{
    public int Iterations = 100;
    public int AntCount = 64;
    public int Goal = 9000;
    public double StartPheromone = 0.1;
    public int CityCount = 200;
    public double DistanceImportance = 2;
    public double PheromoneImportance = 1;
    public double CostImportance;
    public double PathWeightDistance = 1;
    public double PathWeightCost;
    public double EvaporationIntensity = 0.1;
    public int MinDistance = 100;
    public int MaxDistance = 1000;
    public int MinCost = 10;
    public int MaxCost = 500;
    public int ThreadsCount = 8;
}
