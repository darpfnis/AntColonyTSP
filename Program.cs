using AntColonyTSP;

public class Program
{
    public static void Main()
    {
        TspGraph.PrintSampleExample();
        Console.WriteLine();
        new AntColonyTester().RunTests();
    }
}