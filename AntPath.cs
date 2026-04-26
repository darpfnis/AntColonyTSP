namespace AntColonyTSP;

public sealed class AntPath
{
    public int[] Nodes = Array.Empty<int>();
    public int Distance;
    public int Cost;
    public double Objective = double.MaxValue;
}
