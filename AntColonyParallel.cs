namespace AntColonyTSP;

public class AntColonyParallel : AntColony
{
    private readonly ParallelOptions _parallelOptions;

    public AntColonyParallel(int[,] adjacencyMatrix, double[,] pheromoneMatrix, Configurations configurations)
        : base(adjacencyMatrix, pheromoneMatrix, configurations)
    {
        _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.threadsCount };
    }

    public AntPath Solve()
    {
        var paths = new AntPath[Config.antCount];
        AntPath? globalBestPath = null;

        ApplyToMatrix(Config.startPheromone, (current, value) => value);

        for (var iteration = 0; iteration < Config.iterations; iteration++)
        {
            // Фаза 1: кожна мурашка будує свій шлях незалежно — чиста паралельність
            Parallel.For(0, Config.antCount, _parallelOptions, j =>
            {
                paths[j] = BuildAntPath();
            });

            // Фаза 2: знаходимо найкращий шлях ітерації
            foreach (var path in paths)
            {
                if (globalBestPath == null || path.distance < globalBestPath.distance)
                    globalBestPath = path;

                if (path.distance <= Config.goal)
                    return path;
            }

            // Фаза 3: паралельне випаровування — матриця розбивається по рядках
            EvaporatePheromonesParallel();

            // Фаза 4: паралельне накопичення феромонів через локальні матриці
            ApplyPheromonesParallel(paths);
        }

        return globalBestPath ?? paths[0];
    }

    // Будує один повний шлях для однієї мурашки.
    // Використовує масиви замість List щоб уникнути зайвих алокацій.
    private AntPath BuildAntPath()
    {
        // Масив доступних міст — імітуємо видалення через swap з кінцем
        var available = new int[Config.cityCount - 1];
        for (var i = 0; i < available.Length; i++)
            available[i] = i + 1;

        var path = new int[Config.cityCount + 1];
        var remainingCount = available.Length;

        for (var step = 1; step < Config.cityCount; step++)
        {
            var from = path[step - 1];
            var chosenIndex = ChooseNextCity(from, available, remainingCount);

            path[step] = available[chosenIndex];

            // Swap-and-shrink: O(1) замість O(N) RemoveAt
            available[chosenIndex] = available[remainingCount - 1];
            remainingCount--;
        }

        return new AntPath
        {
            path = path.ToList(),
            distance = EvaluateAntPath(path.ToList())
        };
    }

    // Вибирає наступне місто за розподілом імовірностей.
    // Не виділяє List — рахує все на місці.
    private int ChooseNextCity(int from, int[] available, int count)
    {
        double total = 0;
        for (var i = 0; i < count; i++)
        {
            var to = available[i];
            total +=
                Math.Pow(_pheromoneMatrix[from, to], Config.pheromoneImportance) *
                Math.Pow(1.0 / _adjacencyMatrix[from, to], Config.distanceImportance);
        }

        var threshold = Operators.GetRandomDouble(0, total);
        double cumulative = 0;

        for (var i = 0; i < count - 1; i++)
        {
            var to = available[i];
            cumulative +=
                Math.Pow(_pheromoneMatrix[from, to], Config.pheromoneImportance) *
                Math.Pow(1.0 / _adjacencyMatrix[from, to], Config.distanceImportance);

            if (cumulative >= threshold)
                return i;
        }

        return count - 1;
    }

    // Паралельне випаровування: кожен потік обробляє свій діапазон рядків.
    private void EvaporatePheromonesParallel()
    {
        var size = _pheromoneMatrix.GetLength(0);
        var retention = 1.0 - Config.evaporationIntensity;

        Parallel.For(0, size, _parallelOptions, i =>
        {
            for (var j = 0; j < size; j++)
            {
                if (i != j)
                    _pheromoneMatrix[i, j] *= retention;
            }
        });
    }

    // Паралельне накопичення феромонів: кожен потік будує локальну матрицю дельт,
    // після чого всі дельти додаються до спільної матриці одним паралельним проходом.
    private void ApplyPheromonesParallel(AntPath[] paths)
    {
        var size = _pheromoneMatrix.GetLength(0);

        // Кожен потік пише у власну матрицю — без конкуренції
        var threadDeltas = new double[Config.threadsCount][,];
        for (var t = 0; t < Config.threadsCount; t++)
            threadDeltas[t] = new double[size, size];

        Parallel.For(0, Config.antCount, _parallelOptions, j =>
        {
            var threadIndex = j % Config.threadsCount;
            var delta = threadDeltas[threadIndex];
            var path = paths[j];
            var deposit = Config.goal * 1.0 / path.distance;

            for (var k = 0; k < path.path.Count - 1; k++)
                delta[path.path[k], path.path[k + 1]] += deposit;
        });

        // Зливаємо дельти в спільну матрицю — паралельно по рядках
        Parallel.For(0, size, _parallelOptions, i =>
        {
            for (var j = 0; j < size; j++)
            {
                if (i == j) continue;
                double sum = 0;
                for (var t = 0; t < Config.threadsCount; t++)
                    sum += threadDeltas[t][i, j];
                _pheromoneMatrix[i, j] += sum;
            }
        });
    }
}