namespace PaleCheck.Core;

/// <summary>One published image: label, hospital, copy groups and colour features.</summary>
/// <param name="ExactGroup">Images with the same value have pixel-identical photographs.</param>
/// <param name="Cluster">Copy cluster (any kind of copy), or -1 for an image with no copies.</param>
/// <param name="CopyLevel">First audit layer that flags the image: 0 none, 1 identical, 2 near-identical, 3 re-crop.</param>
public record BenchSample(string Id, int Label, int Hospital, int ExactGroup, int Cluster, int CopyLevel, double[] X);

/// <summary>How much of the audit to apply before benchmarking.</summary>
public enum CopyFilter { Published = 0, NoIdentical = 1, NoNearIdentical = 2, NoCopies = 3 }

public enum SplitKind { RandomImages, GroupedCopies, LeaveHospitalOut }

public enum BenchModel { Memoriser, Logistic }

/// <param name="LeakedShare">Share of test images whose pixel-identical twin sits in the training folds.</param>
public record BenchResult(int Images, double Auc, double Accuracy, double LeakedShare);

/// <summary>
/// The Benchmark Lab: cross-validates a memoriser (1-nearest-neighbour) or PaleCheck's logistic model on the
/// published images, with or without the copies, to show how copies inflate scores.
/// </summary>
public static class Benchmark
{
    public const int Folds = 5;
    public const double Lambda = 1.0;

    public static BenchResult Run(IReadOnlyList<BenchSample> all, CopyFilter filter, SplitKind split,
        BenchModel model, int repeats = 3, int seed = 20261001)
    {
        var data = all.Where(s => s.CopyLevel == 0 || s.CopyLevel > (int)filter).ToArray();
        var rng = new Random(seed);
        if (split == SplitKind.LeaveHospitalOut) repeats = 1;
        double aucSum = 0, accSum = 0, leakSum = 0;
        for (int r = 0; r < repeats; r++)
        {
            var scores = new double[data.Length];
            int leaked = 0;
            foreach (var test in MakeFolds(data, split, rng))
            {
                var inTest = new bool[data.Length];
                foreach (var i in test) inTest[i] = true;
                var train = Enumerable.Range(0, data.Length).Where(i => !inTest[i]).ToArray();
                var trainGroups = train.Select(i => data[i].ExactGroup).ToHashSet();
                leaked += test.Count(i => trainGroups.Contains(data[i].ExactGroup));
                Score(data, train, test, model, scores);
            }
            var labels = data.Select(s => s.Label).ToArray();
            aucSum += Stats.Auc(labels, scores);
            accSum += Enumerable.Range(0, data.Length).Count(i => (scores[i] >= 0.5 ? 1 : 0) == labels[i]) / (double)data.Length;
            leakSum += leaked / (double)data.Length;
        }
        return new BenchResult(data.Length, aucSum / repeats, accSum / repeats, leakSum / repeats);
    }

    private static List<int[]> MakeFolds(BenchSample[] data, SplitKind split, Random rng)
    {
        if (split == SplitKind.LeaveHospitalOut)
            return Enumerable.Range(0, data.Length).GroupBy(i => data[i].Hospital).Select(g => g.ToArray()).ToList();

        var fold = new int[data.Length];
        if (split == SplitKind.RandomImages)
        {
            // Stratified: shuffle each class and deal it round-robin.
            foreach (var cls in new[] { 0, 1 })
            {
                var idx = Enumerable.Range(0, data.Length).Where(i => data[i].Label == cls).ToArray();
                rng.Shuffle(idx);
                for (int k = 0; k < idx.Length; k++) fold[idx[k]] = k % Folds;
            }
        }
        else
        {
            // Every copy of a photograph lands in the same fold.
            var keys = Enumerable.Range(0, data.Length).Select(i => data[i].Cluster >= 0 ? data[i].Cluster : 1_000_000 + i).ToArray();
            var unique = keys.Distinct().ToArray();
            rng.Shuffle(unique);
            var foldOf = unique.Select((k, n) => (k, n)).ToDictionary(t => t.k, t => t.n % Folds);
            for (int i = 0; i < data.Length; i++) fold[i] = foldOf[keys[i]];
        }
        return Enumerable.Range(0, Folds).Select(f => Enumerable.Range(0, data.Length).Where(i => fold[i] == f).ToArray())
            .Where(f => f.Length > 0).ToList();
    }

    private static void Score(BenchSample[] data, int[] train, int[] test, BenchModel model, double[] scores)
    {
        int d = data[0].X.Length;
        var mean = new double[d];
        var sd = new double[d];
        foreach (var i in train) for (int k = 0; k < d; k++) mean[k] += data[i].X[k];
        for (int k = 0; k < d; k++) mean[k] /= train.Length;
        foreach (var i in train) for (int k = 0; k < d; k++) sd[k] += (data[i].X[k] - mean[k]) * (data[i].X[k] - mean[k]);
        for (int k = 0; k < d; k++) sd[k] = sd[k] > 0 ? Math.Sqrt(sd[k] / train.Length) : 1;
        double[] Z(int i) => Enumerable.Range(0, d).Select(k => (data[i].X[k] - mean[k]) / sd[k]).ToArray();

        var zTrain = train.Select(Z).ToArray();
        if (model == BenchModel.Logistic)
        {
            var (w, b) = Stats.FitLogistic(zTrain, train.Select(i => data[i].Label).ToArray(), Lambda);
            foreach (var i in test) scores[i] = Stats.PredictLogistic(Z(i), w, b);
            return;
        }
        // Memoriser: copy the label of the closest training image (average over exact ties).
        foreach (var i in test)
        {
            var z = Z(i);
            double best = double.MaxValue, sum = 0;
            int count = 0;
            for (int t = 0; t < train.Length; t++)
            {
                double dist = 0;
                for (int k = 0; k < d; k++) dist += (z[k] - zTrain[t][k]) * (z[k] - zTrain[t][k]);
                if (dist < best - 1e-12) { best = dist; sum = data[train[t]].Label; count = 1; }
                else if (Math.Abs(dist - best) <= 1e-12) { sum += data[train[t]].Label; count++; }
            }
            scores[i] = sum / count;
        }
    }
}
