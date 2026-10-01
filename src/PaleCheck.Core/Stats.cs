namespace PaleCheck.Core;

/// <summary>Small statistics helpers shared by the Benchmark Lab and the tests.</summary>
public static class Stats
{
    /// <summary>
    /// Area under the ROC curve: the chance that a random positive scores above a random negative
    /// (ties count half). NaN when one class is missing.
    /// </summary>
    public static double Auc(IReadOnlyList<int> labels, IReadOnlyList<double> scores)
    {
        int n = labels.Count;
        var order = Enumerable.Range(0, n).OrderBy(i => scores[i]).ToArray();
        var ranks = new double[n];
        for (int i = 0; i < n;)
        {
            int j = i;
            while (j + 1 < n && scores[order[j + 1]] == scores[order[i]]) j++;
            for (int k = i; k <= j; k++) ranks[order[k]] = (i + j) / 2.0 + 1;
            i = j + 1;
        }
        double pos = 0, rankSum = 0;
        for (int i = 0; i < n; i++)
        {
            if (labels[i] != 1) continue;
            pos++;
            rankSum += ranks[i];
        }
        double neg = n - pos;
        if (pos == 0 || neg == 0) return double.NaN;
        return (rankSum - pos * (pos + 1) / 2) / (pos * neg);
    }

    /// <summary>L2-regularised logistic regression by Newton's method on standardised inputs (bias not shrunk).</summary>
    public static (double[] Weights, double Bias) FitLogistic(double[][] x, int[] y, double lambda)
    {
        int n = x.Length, d = x[0].Length, m = d + 1;
        var w = new double[m];
        for (int iter = 0; iter < 50; iter++)
        {
            var grad = new double[m];
            var h = new double[m, m];
            for (int i = 0; i < n; i++)
            {
                double z = w[d];
                for (int k = 0; k < d; k++) z += w[k] * x[i][k];
                double p = 1 / (1 + Math.Exp(-z)), r = p - y[i], s = p * (1 - p);
                for (int a = 0; a < m; a++)
                {
                    double xa = a < d ? x[i][a] : 1;
                    grad[a] += r * xa;
                    for (int b = 0; b < m; b++) h[a, b] += s * xa * (b < d ? x[i][b] : 1);
                }
            }
            for (int a = 0; a < d; a++)
            {
                grad[a] += lambda * w[a];
                h[a, a] += lambda;
            }
            for (int a = 0; a < m; a++) h[a, a] += 1e-9;
            var step = Solve(h, grad);
            double max = 0;
            for (int a = 0; a < m; a++)
            {
                w[a] -= step[a];
                max = Math.Max(max, Math.Abs(step[a]));
            }
            if (max < 1e-10) break;
        }
        return (w[..d], w[d]);
    }

    public static double PredictLogistic(double[] x, double[] weights, double bias)
    {
        double z = bias;
        for (int k = 0; k < x.Length; k++) z += weights[k] * x[k];
        return 1 / (1 + Math.Exp(-z));
    }

    /// <summary>Gaussian elimination with partial pivoting (small dense systems).</summary>
    private static double[] Solve(double[,] a, double[] b)
    {
        int n = b.Length;
        var m = (double[,])a.Clone();
        var v = (double[])b.Clone();
        for (int c = 0; c < n; c++)
        {
            int p = c;
            for (int r = c + 1; r < n; r++) if (Math.Abs(m[r, c]) > Math.Abs(m[p, c])) p = r;
            if (p != c)
            {
                for (int k = 0; k < n; k++) (m[c, k], m[p, k]) = (m[p, k], m[c, k]);
                (v[c], v[p]) = (v[p], v[c]);
            }
            for (int r = c + 1; r < n; r++)
            {
                double f = m[r, c] / m[c, c];
                for (int k = c; k < n; k++) m[r, k] -= f * m[c, k];
                v[r] -= f * v[c];
            }
        }
        var x = new double[n];
        for (int r = n - 1; r >= 0; r--)
        {
            double s = v[r];
            for (int k = r + 1; k < n; k++) s -= m[r, k] * x[k];
            x[r] = s / m[r, r];
        }
        return x;
    }
}
