using System.Text.Json;
using PaleCheck.Core;

namespace PaleCheck.Core.Tests;

/// <summary>
/// The browser must compute exactly what model/train.py validated. train.py writes raw pixels
/// plus its own features and probabilities to parity/; these tests recompute them in C#.
/// </summary>
public class ParityTests
{
    private record Fixture(string File, int Width, int Height, int ValidPixels, double[] Features,
        double Probability, int[] WhiteRgb, double[] FeaturesWhiteBalanced);

    private static readonly PallorModel Model = PallorModel.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "model.json")));

    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();
        foreach (var f in Load()) data.Add(f.File);
        return data;
    }

    private static List<Fixture> Load() =>
        JsonSerializer.Deserialize<List<Fixture>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "parity", "fixtures.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static (Fixture Fx, byte[] Rgba) Get(string file)
    {
        var fx = Load().Single(f => f.File == file);
        return (fx, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "parity", file)));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Features_match_python(string file)
    {
        var (fx, rgba) = Get(file);
        Assert.Equal(fx.Width * fx.Height * 4, rgba.Length);

        var (rgb, counts) = FeatureExtractor.ValidPixels(rgba, Model.PixelRules);
        Assert.Equal(fx.ValidPixels, counts.Valid);

        var features = FeatureExtractor.Extract(rgb);
        Assert.Equal(FeatureExtractor.All.Length, features.Length);
        for (int i = 0; i < features.Length; i++)
            Assert.True(Math.Abs(features[i] - fx.Features[i]) < 1e-9,
                $"{FeatureExtractor.All[i]}: C# {features[i]} vs Python {fx.Features[i]}");
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Probability_matches_python(string file)
    {
        var (fx, rgba) = Get(file);
        var (rgb, _) = FeatureExtractor.ValidPixels(rgba, Model.PixelRules);
        // model.json stores parameters rounded to 6 decimals, so allow a small tolerance.
        Assert.Equal(fx.Probability, Model.Predict(FeatureExtractor.Extract(rgb)), 3);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void White_balanced_features_match_python(string file)
    {
        var (fx, rgba) = Get(file);
        var (rgb, _) = FeatureExtractor.ValidPixels(rgba, Model.PixelRules);
        var wb = ColorMath.WhiteBalance(rgb, (fx.WhiteRgb[0], fx.WhiteRgb[1], fx.WhiteRgb[2]));
        var features = FeatureExtractor.Extract(wb);
        for (int i = 0; i < features.Length; i++)
            Assert.True(Math.Abs(features[i] - fx.FeaturesWhiteBalanced[i]) < 1e-9,
                $"{FeatureExtractor.All[i]}: C# {features[i]} vs Python {fx.FeaturesWhiteBalanced[i]}");
    }
}
