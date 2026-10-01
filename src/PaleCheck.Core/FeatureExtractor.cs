namespace PaleCheck.Core;

/// <summary>Pixel filtering and colour features of the eyelid. Mirrors model/features.py exactly.</summary>
public static class FeatureExtractor
{
    public static readonly string[] Absolute =
        ["a_mean", "a_p25", "L_mean", "b_mean", "ei_mean", "r_chrom", "g_chrom", "sat_mean", "a_std"];

    public static readonly string[] Relative =
        ["a_p90_minus_p10", "a_p90_minus_p50", "ei_p90_minus_p10", "ei_p90_minus_p50",
         "a_over_L", "a_cv", "frac_ei_above_median_plus5", "rg_top20_minus_bottom20"];

    public static readonly string[] All = [.. Absolute, .. Relative];

    /// <summary>Counts of what happened to the outlined pixels, used by the quality checks.</summary>
    public record PixelCounts(int Outlined, int Glare, int Dark, int Valid);

    /// <summary>
    /// Keeps outlined (opaque) pixels that are neither specular glare nor shadow.
    /// Input is RGBA bytes, row-major; output is RGB triples as doubles.
    /// </summary>
    public static (double[] Rgb, PixelCounts Counts) ValidPixels(ReadOnlySpan<byte> rgba, PixelRules rules)
    {
        var keep = new List<double>(rgba.Length / 4 * 3);
        int outlined = 0, glare = 0, dark = 0;
        for (int i = 0; i + 3 < rgba.Length; i += 4)
        {
            if (rgba[i + 3] < rules.AlphaMin) continue;
            outlined++;
            byte r = rgba[i], g = rgba[i + 1], b = rgba[i + 2];
            if (Math.Min(r, Math.Min(g, b)) > rules.GlareMin) { glare++; continue; }
            if (Math.Max(r, Math.Max(g, b)) < rules.DarkMax) { dark++; continue; }
            keep.Add(r);
            keep.Add(g);
            keep.Add(b);
        }
        return (keep.ToArray(), new PixelCounts(outlined, glare, dark, keep.Count / 3));
    }

    /// <summary>All features in <see cref="All"/> order from RGB triples (0..255).</summary>
    public static double[] Extract(double[] rgb)
    {
        int n = rgb.Length / 3;
        if (n == 0) throw new ArgumentException("No usable pixels.", nameof(rgb));

        var L = new double[n];
        var a = new double[n];
        var ei = new double[n];
        var rg = new double[n];
        double sumA = 0, sumL = 0, sumB = 0, sumEi = 0, sumR = 0, sumG = 0, sumSat = 0, sumAOverL = 0;
        for (int i = 0; i < n; i++)
        {
            double r = rgb[3 * i], g = rgb[3 * i + 1], b = rgb[3 * i + 2];
            var lab = ColorMath.RgbToLab(r, g, b);
            L[i] = lab.L;
            a[i] = lab.A;
            double s = r + g + b + 1e-9;
            double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
            ei[i] = 100.0 * Math.Log10((r + 1.0) / (g + 1.0));
            rg[i] = r / (g + 1.0);
            sumA += lab.A;
            sumL += lab.L;
            sumB += lab.B;
            sumEi += ei[i];
            sumR += r / s;
            sumG += g / s;
            sumSat += mx > 0 ? (mx - mn) / Math.Max(mx, 1e-9) : 0.0;
            sumAOverL += lab.A / Math.Max(lab.L, 1.0);
        }

        double aMean = sumA / n;
        double varA = 0;
        for (int i = 0; i < n; i++) varA += (a[i] - aMean) * (a[i] - aMean);
        double aStd = Math.Sqrt(varA / n);

        var sa = (double[])a.Clone();
        var se = (double[])ei.Clone();
        Array.Sort(sa);
        Array.Sort(se);
        Array.Sort(rg);
        double aP10 = Percentile(sa, 0.10), aP25 = Percentile(sa, 0.25), aP50 = Percentile(sa, 0.50), aP90 = Percentile(sa, 0.90);
        double eP10 = Percentile(se, 0.10), eP50 = Percentile(se, 0.50), eP90 = Percentile(se, 0.90);

        int above = 0;
        for (int i = 0; i < n; i++) if (ei[i] > eP50 + 5.0) above++;

        int k = Math.Max(n / 5, 1);
        double top = 0, bottom = 0;
        for (int i = 0; i < k; i++)
        {
            bottom += rg[i];
            top += rg[n - k + i];
        }

        return
        [
            aMean, aP25, sumL / n, sumB / n, sumEi / n, sumR / n, sumG / n, sumSat / n, aStd,
            aP90 - aP10,
            aP90 - aP50,
            eP90 - eP10,
            eP90 - eP50,
            sumAOverL / n,
            aStd / Math.Max(Math.Abs(aMean), 1e-6),
            (double)above / n,
            top / k - bottom / k,
        ];
    }

    /// <summary>Linear-interpolated percentile of sorted values (numpy's default method).</summary>
    public static double Percentile(double[] sorted, double q)
    {
        double pos = (sorted.Length - 1) * q;
        int lo = (int)Math.Floor(pos);
        int hi = Math.Min(lo + 1, sorted.Length - 1);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
    }
}
