using System.Text.Json;

namespace PaleCheck.Core;

public record PixelRules(int AlphaMin, int GlareMin, int DarkMax);

public record Thresholds(double Low, double High);

public record ReferenceDistribution(int[] HealthyHistogram, int[] AnaemicHistogram);

/// <summary>
/// The logistic-regression model exported by model/train.py (wwwroot/model/model.json).
/// </summary>
public record PallorModel(
    string Version,
    string TrainedOn,
    string FeatureSet,
    string[] Features,
    double[] Mean,
    double[] Std,
    double[] Weights,
    double Bias,
    double Lambda,
    Thresholds Thresholds,
    PixelRules PixelRules,
    ReferenceDistribution Reference)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PallorModel FromJson(string json) =>
        JsonSerializer.Deserialize<PallorModel>(json, Options)
        ?? throw new InvalidDataException("model.json is empty.");

    /// <summary>Probability-like score that the eyelid belongs to the anaemic group.</summary>
    public double Predict(double[] allFeatures)
    {
        double z = Bias;
        for (int i = 0; i < Features.Length; i++)
        {
            int idx = Array.IndexOf(FeatureExtractor.All, Features[i]);
            if (idx < 0) throw new InvalidDataException($"Unknown feature '{Features[i]}' in model.json.");
            z += Weights[i] * (allFeatures[idx] - Mean[i]) / Std[i];
        }
        return 1.0 / (1.0 + Math.Exp(-z));
    }

    public PallorBand BandFor(double probability) =>
        probability >= Thresholds.High ? PallorBand.Higher
        : probability >= Thresholds.Low ? PallorBand.InBetween
        : PallorBand.Lower;
}

public enum PallorBand { Lower, InBetween, Higher }
