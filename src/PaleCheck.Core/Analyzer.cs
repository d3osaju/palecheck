namespace PaleCheck.Core;

public enum QualityIssue
{
    // Blocking: the photo cannot be measured.
    OutlineTooSmall,
    TooMuchGlare,
    TooDark,
    // Warnings: measured, but less trustworthy.
    NoCard,
    CardOverexposed,
    CardTooDark,
    StrongColouredLight,
}

public record Rgb(double R, double G, double B);

public record AnalysisResult(
    bool Measured,
    IReadOnlyList<QualityIssue> Issues,
    FeatureExtractor.PixelCounts Counts,
    double Probability,
    int PallorIndex,
    PallorBand Band,
    double[] Features,
    double RawAStar,
    double CorrectedAStar,
    double CorrectedLStar)
{
    public static bool IsBlocking(QualityIssue issue) => issue <= QualityIssue.TooDark;
}

/// <summary>Turns the outlined eyelid pixels (and optional white-card sample) into a measurement.</summary>
public static class Analyzer
{
    public const int MinOutlinedPixels = 400;
    public const int MinValidPixels = 300;
    public const double MaxGlareShare = 0.25;
    public const double MaxDarkShare = 0.25;

    public static AnalysisResult Analyze(ReadOnlySpan<byte> rgba, PallorModel model, Rgb? cardWhite)
    {
        var (rgb, counts) = FeatureExtractor.ValidPixels(rgba, model.PixelRules);
        var issues = new List<QualityIssue>();

        if (counts.Outlined < MinOutlinedPixels || counts.Valid < MinValidPixels)
            issues.Add(QualityIssue.OutlineTooSmall);
        if (counts.Outlined > 0 && (double)counts.Glare / counts.Outlined > MaxGlareShare)
            issues.Add(QualityIssue.TooMuchGlare);
        if (counts.Outlined > 0 && (double)counts.Dark / counts.Outlined > MaxDarkShare)
            issues.Add(QualityIssue.TooDark);

        if (cardWhite is null)
        {
            issues.Add(QualityIssue.NoCard);
        }
        else
        {
            double max = Math.Max(cardWhite.R, Math.Max(cardWhite.G, cardWhite.B));
            double min = Math.Min(cardWhite.R, Math.Min(cardWhite.G, cardWhite.B));
            if (max >= 250) issues.Add(QualityIssue.CardOverexposed);
            if (max < 60) issues.Add(QualityIssue.CardTooDark);
            if (min > 0 && max / min > 2.2) issues.Add(QualityIssue.StrongColouredLight);
        }

        if (issues.Any(AnalysisResult.IsBlocking))
            return new AnalysisResult(false, issues, counts, 0, 0, PallorBand.Lower, [], 0, 0, 0);

        double rawA = FeatureExtractor.Extract(rgb)[0];
        var corrected = cardWhite is null ? rgb : ColorMath.WhiteBalance(rgb, (cardWhite.R, cardWhite.G, cardWhite.B));
        var features = FeatureExtractor.Extract(corrected);
        double p = model.Predict(features);
        return new AnalysisResult(true, issues, counts, p, (int)Math.Round(p * 100), model.BandFor(p),
            features, rawA, features[0], features[2]);
    }

    /// <summary>
    /// Lighting Lab: the same target photographed under several lights. Returns the spread
    /// (max - min) of a* before and after card correction.
    /// </summary>
    public static (double RawSpread, double CorrectedSpread) LightingSpread(IEnumerable<AnalysisResult> shots)
    {
        var measured = shots.Where(s => s.Measured).ToList();
        if (measured.Count < 2) return (0, 0);
        return (measured.Max(s => s.RawAStar) - measured.Min(s => s.RawAStar),
                measured.Max(s => s.CorrectedAStar) - measured.Min(s => s.CorrectedAStar));
    }
}
