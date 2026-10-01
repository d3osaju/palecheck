using PaleCheck.Core;

namespace PaleCheck.Core.Tests;

public class AnalyzerTests
{
    private static readonly PallorModel Model = PallorModel.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "model.json")));

    /// <summary>A w*h patch of one colour, fully opaque.</summary>
    private static byte[] Patch(int count, byte r, byte g, byte b, byte alpha = 255)
    {
        var px = new byte[count * 4];
        for (int i = 0; i < count; i++)
        {
            px[4 * i] = r;
            px[4 * i + 1] = g;
            px[4 * i + 2] = b;
            px[4 * i + 3] = alpha;
        }
        return px;
    }

    private static byte[] Tissue(int count = 2000)
    {
        // Pinkish-red with some variation, like a conjunctiva.
        var px = new byte[count * 4];
        var rng = new Random(1);
        for (int i = 0; i < count; i++)
        {
            px[4 * i] = (byte)rng.Next(170, 230);
            px[4 * i + 1] = (byte)rng.Next(80, 130);
            px[4 * i + 2] = (byte)rng.Next(80, 120);
            px[4 * i + 3] = 255;
        }
        return px;
    }

    [Fact]
    public void Tiny_outline_is_rejected()
    {
        var r = Analyzer.Analyze(Patch(100, 200, 100, 100), Model, new Rgb(240, 240, 240));
        Assert.False(r.Measured);
        Assert.Contains(QualityIssue.OutlineTooSmall, r.Issues);
    }

    [Fact]
    public void Transparent_pixels_are_outside_the_outline()
    {
        var r = Analyzer.Analyze(Patch(5000, 200, 100, 100, alpha: 0), Model, new Rgb(240, 240, 240));
        Assert.Equal(0, r.Counts.Outlined);
        Assert.False(r.Measured);
    }

    [Fact]
    public void Mostly_glare_is_rejected()
    {
        var px = Patch(2000, 250, 250, 250).Concat(Tissue(500)).ToArray();
        var r = Analyzer.Analyze(px, Model, new Rgb(240, 240, 240));
        Assert.False(r.Measured);
        Assert.Contains(QualityIssue.TooMuchGlare, r.Issues);
    }

    [Fact]
    public void Mostly_shadow_is_rejected()
    {
        var px = Patch(2000, 10, 10, 10).Concat(Tissue(500)).ToArray();
        var r = Analyzer.Analyze(px, Model, new Rgb(240, 240, 240));
        Assert.False(r.Measured);
        Assert.Contains(QualityIssue.TooDark, r.Issues);
    }

    [Fact]
    public void Good_photo_is_measured_and_missing_card_is_a_warning()
    {
        var r = Analyzer.Analyze(Tissue(), Model, cardWhite: null);
        Assert.True(r.Measured);
        Assert.Contains(QualityIssue.NoCard, r.Issues);
        Assert.InRange(r.Probability, 0, 1);
        Assert.Equal((int)Math.Round(r.Probability * 100), r.PallorIndex);
    }

    [Fact]
    public void Card_warnings()
    {
        Assert.Contains(QualityIssue.CardOverexposed, Analyzer.Analyze(Tissue(), Model, new Rgb(255, 255, 255)).Issues);
        Assert.Contains(QualityIssue.CardTooDark, Analyzer.Analyze(Tissue(), Model, new Rgb(40, 40, 40)).Issues);
        Assert.Contains(QualityIssue.StrongColouredLight, Analyzer.Analyze(Tissue(), Model, new Rgb(240, 160, 80)).Issues);
        Assert.Empty(Analyzer.Analyze(Tissue(), Model, new Rgb(230, 228, 225)).Issues);
    }

    [Fact]
    public void Neutral_card_changes_nothing()
    {
        var plain = Analyzer.Analyze(Tissue(), Model, new Rgb(200, 200, 200));
        Assert.Equal(plain.RawAStar, plain.CorrectedAStar, 9);
    }

    [Fact]
    public void Card_cancels_a_warm_light()
    {
        // Simulate the same tissue under a warm bulb: multiply linear channels by the light colour.
        var light = (R: 1.0, G: 0.70, B: 0.40);
        var tissue = Tissue();
        var warm = (byte[])tissue.Clone();
        for (int i = 0; i < warm.Length; i += 4)
        {
            warm[i] = Relight(tissue[i], light.R);
            warm[i + 1] = Relight(tissue[i + 1], light.G);
            warm[i + 2] = Relight(tissue[i + 2], light.B);
        }
        var cardUnderWarm = new Rgb(Relight(255, light.R), Relight(255, light.G), Relight(255, light.B));

        var daylight = Analyzer.Analyze(tissue, Model, new Rgb(255, 255, 255) with { R = 245, G = 245, B = 245 });
        var uncorrected = Analyzer.Analyze(warm, Model, cardWhite: null);
        var corrected = Analyzer.Analyze(warm, Model, cardUnderWarm);

        double before = Math.Abs(uncorrected.CorrectedAStar - daylight.CorrectedAStar);
        double after = Math.Abs(corrected.CorrectedAStar - daylight.CorrectedAStar);
        Assert.True(before > 5, $"warm light should shift a* a lot, shifted {before}");
        Assert.True(after < 1, $"card should cancel most of it, left {after}");

        // Lighting Lab: every shot includes the card, so compare daylight vs. warm, both carded.
        var (raw, fixedSpread) = Analyzer.LightingSpread([daylight, corrected]);
        Assert.True(raw > 5 && fixedSpread < 1, $"raw spread {raw}, corrected spread {fixedSpread}");

        static byte Relight(byte v, double gain) =>
            (byte)Math.Round(ColorMath.LinearToSrgb(ColorMath.SrgbToLinear(v / 255.0) * gain) * 255.0);
    }

    [Theory]
    [InlineData(0.0, PallorBand.Lower)]
    [InlineData(0.999, PallorBand.Higher)]
    public void Bands_follow_thresholds(double p, PallorBand expected)
    {
        Assert.Equal(expected, Model.BandFor(p));
        Assert.Equal(PallorBand.InBetween, Model.BandFor((Model.Thresholds.Low + Model.Thresholds.High) / 2));
        Assert.Equal(PallorBand.Higher, Model.BandFor(Model.Thresholds.High));
    }
}
