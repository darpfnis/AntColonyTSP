namespace AntColonyTSP;

public class AntColonyParallel : AntColony
{
    // Кожен потік має свій Random без contention
    private static readonly ThreadLocal<Random> _threadRandom =
        new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

    public AntColonyParallel(int[,] adjacencyMatrix, double[,] pheromoneMatrix, Configurations configurations)
        : base(adjacencyMatrix, pheromoneMatrix, configurations)
    {
    }

    public AntPath Solve()
    {
        AntPath? globalBestPath = null;
        var lockObject = new object();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.threadsCount };
        var size = Config.cityCount;

        ApplyToMatrix(Config.startPheromone, (_, value) => value);

        for (var i = 0; i < Config.iterations; i++)
        {
            // Кожен потік збирає свій delta без lock на запис феромонів
            var deltaMatrices = new double[Config.threadsCount][,];
            for (int t = 0; t < Config.threadsCount; t++)
                deltaMatrices[t] = new double[size, size];

            var foundGoal = false;
            AntPath? iterationBest = null;

            Parallel.For(0, Config.antCount, parallelOptions,
                () => (localBest: new AntPath { distance = int.MaxValue }, 
                       delta: deltaMatrices[Environment.CurrentManagedThreadId % Config.threadsCount]),
                (j, state, local) =>
                {
                    if (state.IsStopped) return local;

                    var rng = _threadRandom.Value!;

                    // Swap-and-shrink замість List.RemoveAt, O(1) видалення
                    var dirs = new int[size - 1];
                    for (int c = 0; c < size - 1; c++) dirs[c] = c + 1;
                    var dirsCount = size - 1;

                    var path = new int[size + 1];
                    // path[0] = 0 за замовчуванням

                    for (var k = 1; k < size; k++)
                    {
                        var dirSpan = dirs.AsSpan(0, dirsCount);
                        var distribution = CalculateProbabilityDistributionSpan(path[k - 1], dirSpan);
                        var probability = rng.NextDouble();
                        var index = GetIndexByProbability(distribution, probability);

                        path[k] = dirSpan[index];

                        // O(1) свапаємо з останнім і зменшуємо лічильник
                        dirs[index] = dirs[--dirsCount];
                    }
                    path[size] = 0;

                    var distance = EvaluateAntPathArray(path);

                    if (distance <= Config.goal)
                    {
                        local.localBest = new AntPath { path = path.ToList(), distance = distance };
                        state.Stop();
                        return local;
                    }

                    // Записуємо delta феромонів у локальну матрицю потоку 
                    var contribution = (double)Config.goal / distance;
                    for (var p = 0; p < size; p++)
                        local.delta[path[p], path[p + 1]] += contribution;

                    if (distance < local.localBest.distance)
                        local.localBest = new AntPath { path = path.ToList(), distance = distance };

                    return local;
                },
                (finalLocal) =>
                {
                    lock (lockObject)
                    {
                        if (globalBestPath == null || finalLocal.localBest.distance < globalBestPath.distance)
                            globalBestPath = finalLocal.localBest;
                        if (finalLocal.localBest.distance <= Config.goal)
                            foundGoal = true;
                    }
                }
            );

            if (foundGoal && globalBestPath != null)
                return globalBestPath;

            // Паралельне випаровування по рядках матриці
            var evapFactor = 1.0 - Config.evaporationIntensity;
            Parallel.For(0, size, parallelOptions, row =>
            {
                for (var col = 0; col < size; col++)
                {
                    if (row == col) continue;
                    var pheromone = _pheromoneMatrix[row, col] * evapFactor;

                    // Merge усіх delta-матриць для цієї комірки
                    for (var t = 0; t < Config.threadsCount; t++)
                        pheromone += deltaMatrices[t][row, col];

                    _pheromoneMatrix[row, col] = pheromone;
                }
            });
        }

        return globalBestPath ?? new AntPath();
    }

    // Версія з Span( уникає алокацій List кожної ітерації )
    private double[] CalculateProbabilityDistributionSpan(int from, Span<int> dirs)
    {
        var probs = new double[dirs.Length];
        double total = 0;

        for (var i = 0; i < dirs.Length; i++)
        {
            var to = dirs[i];
            probs[i] = Math.Pow(_pheromoneMatrix[from, to], Config.pheromoneImportance) *
                       Math.Pow(1.0 / _adjacencyMatrix[from, to], Config.distanceImportance);
            total += probs[i];
        }

        double sum = 0;
        for (var i = 0; i < probs.Length; i++)
            probs[i] = (sum += probs[i] / total);
        probs[^1] = 1.0;

        return probs;
    }

    private static int GetIndexByProbability(double[] dist, double p)
    {
        for (var i = 0; i < dist.Length; i++)
            if (p <= dist[i]) return i;
        return dist.Length - 1;
    }

    // Версія з масивом замість List
    private int EvaluateAntPathArray(int[] path)
    {
        var d = 0;
        for (var i = 0; i < path.Length - 1; i++)
            d += _adjacencyMatrix[path[i], path[i + 1]];
        return d;
    }
}