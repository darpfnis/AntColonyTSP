namespace AntColonyTSP;

public class AntPath
{
    public List<int> path { get; set; }
    public int distance { get; set; }

    public AntPath()
    {
        path = new List<int>();
        distance = 0;
    }
}