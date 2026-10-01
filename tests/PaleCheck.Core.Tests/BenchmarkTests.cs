using System.Text.Json;
using PaleCheck.Core;

namespace PaleCheck.Core.Tests;

public class BenchmarkTests
{
    private record Row(string Id, int Y, int H, int E, int C, int K, double[] X);
    private record FeatureFile(string[] Features, string[] Hospitals, Row[] Rows);

    private static readonly Lazy<BenchSample[]> Published = new(() =>
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cp-anemic-features.json"));
        var f = JsonSerializer.Deserialize<FeatureFile>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        return f.Rows.Select(r => new BenchSample(r.Id, r.Y, r.H, r.E, r.C, r.K, r.X)).ToArray();
    });

    /// <summary>Pure noise, then half the images copied twice under new ids.</summary>
    private static BenchSample[] NoiseWithCopies()
    {
        var rng = new Random(7);
        var list = new List<BenchSample>();
        for (int i = 0; i < 300; i++)
        {
            var x = Enumerable.Range(0, 5).Select(_ => rng.NextDouble()).ToArray();
            int label = rng.Next(2);
            bool copied = i < 150;
            for (int c = 0; c < (copied ? 3 : 1); c++)
                list.Add(new BenchSample($"{i}-{c}", label, i % 4, i, copied ? i : -1, copied ? 1 : 0, x));
        }
        return list.ToArray();
    }

    [Fact]
    public void Memoriser_cheats_on_copies_of_noise()
    {
        var data = NoiseWithCopies();
        var leaky = Benchmark.Run(data, CopyFilter.Published, SplitKind.RandomImages, BenchModel.Memoriser);
        var grouped = Benchmark.Run(data, CopyFilter.Published, SplitKind.GroupedCopies, BenchModel.Memoriser);
        var clean = Benchmark.Run(data, CopyFilter.NoCopies, SplitKind.RandomImages, BenchModel.Memoriser);
        Assert.True(leaky.Auc > 0.7, $"copies should leak: {leaky.Auc}");
        Assert.True(leaky.LeakedShare > 0.5);
        Assert.InRange(grouped.Auc, 0.35, 0.65);
        Assert.InRange(clean.Auc, 0.35, 0.65);
        Assert.Equal(0, grouped.LeakedShare);
    }

    [Fact]
    public void Reproduces_the_research_findings_on_the_real_features()
    {
        var data = Published.Value;
        Assert.Equal(710, data.Length);
        var memPublished = Benchmark.Run(data, CopyFilter.Published, SplitKind.RandomImages, BenchModel.Memoriser);
        var memClean = Benchmark.Run(data, CopyFilter.NoCopies, SplitKind.RandomImages, BenchModel.Memoriser);
        var logClean = Benchmark.Run(data, CopyFilter.NoCopies, SplitKind.RandomImages, BenchModel.Logistic);
        Assert.True(memPublished.Auc > 0.8, $"memoriser on published data: {memPublished.Auc}");
        Assert.True(memPublished.LeakedShare > 0.35);
        Assert.True(memClean.Auc < 0.66, $"memoriser without copies: {memClean.Auc}");
        Assert.InRange(logClean.Auc, 0.58, 0.75);
        Assert.Equal(data.Count(s => s.CopyLevel == 0), memClean.Images);
    }

    [Fact]
    public void Leave_hospital_out_uses_every_image_once()
    {
        var r = Benchmark.Run(Published.Value, CopyFilter.NoIdentical, SplitKind.LeaveHospitalOut, BenchModel.Logistic);
        Assert.Equal(Published.Value.Count(s => s.CopyLevel is 0 or > 1), r.Images);
        Assert.InRange(r.Auc, 0.4, 0.9);
    }

    [Fact]
    public void Auc_basics()
    {
        Assert.Equal(1.0, Stats.Auc([0, 0, 1, 1], [0.1, 0.2, 0.8, 0.9]));
        Assert.Equal(0.0, Stats.Auc([1, 1, 0, 0], [0.1, 0.2, 0.8, 0.9]));
        Assert.Equal(0.5, Stats.Auc([0, 1, 0, 1], [0.5, 0.5, 0.5, 0.5]));
        Assert.True(double.IsNaN(Stats.Auc([1, 1], [0.1, 0.2])));
    }
}

public class ScreeningAndColourTests
{
    [Fact]
    public void Phone_first_per_thousand()
    {
        var r = Screening.PhoneFirst(0.5, 0.9, 0.8);
        Assert.Equal(new ScreeningOutcome(500, 450, 50, 100, 550), r);
        var all = Screening.TestEveryone(0.591);
        Assert.Equal(591, all.Found);
        Assert.Equal(1000, all.BloodTests);
        Assert.Equal(0, all.Missed);
    }

    [Theory]
    [InlineData(200, 100, 100)]
    [InlineData(240, 180, 150)]
    [InlineData(30, 60, 200)]
    [InlineData(128, 128, 128)]
    public void Lab_round_trips_to_rgb(double r, double g, double b)
    {
        var lab = ColorMath.RgbToLab(r, g, b);
        var back = ColorMath.LabToRgb(lab.L, lab.A, lab.B);
        Assert.InRange(back.R, r - 0.5, r + 0.5);
        Assert.InRange(back.G, g - 0.5, g + 0.5);
        Assert.InRange(back.B, b - 0.5, b + 0.5);
    }
}
