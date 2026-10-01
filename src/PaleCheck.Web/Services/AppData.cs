using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaleCheck.Core;

namespace PaleCheck.Web.Services;

public record Sample(string File, string SourceId, double Hb, bool Anaemic);

public record Confusion(double Threshold, int Tp, int Fn, int Tn, int Fp, double Sensitivity, double Specificity,
    double Ppv, double Npv, double Accuracy);

public record AuditExample(string Id, double Hb,
    [property: JsonPropertyName("age_months")] double AgeMonths, string Gender, string Hospital);

public record Audit(
    [property: JsonPropertyName("images_in_sheet_and_folders")] int Images,
    [property: JsonPropertyName("duplicate_groups")] int DuplicateGroups,
    [property: JsonPropertyName("images_in_duplicate_groups")] int ImagesInDuplicateGroups,
    [property: JsonPropertyName("share_of_dataset_in_duplicate_groups")] double Share,
    [property: JsonPropertyName("largest_group_size")] int LargestGroup,
    [property: JsonPropertyName("groups_whose_copies_list_different_patients")] int GroupsDifferentPatients,
    [property: JsonPropertyName("groups_with_conflicting_labels")] int GroupsConflicting,
    [property: JsonPropertyName("max_hb_spread_within_one_photo_g_dl")] double MaxHbSpread,
    [property: JsonPropertyName("largest_group_example")] AuditExample[] Example,
    [property: JsonPropertyName("random_5fold_test_images_with_identical_twin_in_train")] double LeakedShare,
    [property: JsonPropertyName("memoriser_accuracy_under_random_5fold")] double MemoriserAccuracy,
    [property: JsonPropertyName("published_benchmark_for_context")] string PublishedBenchmark,
    [property: JsonPropertyName("near_identical_pairs")] int NearPairs,
    [property: JsonPropertyName("near_identical_rule")] string NearRule,
    [property: JsonPropertyName("images_added_by_near_identical")] int NearAdded,
    [property: JsonPropertyName("clusters_with_near_identical_copies")] int NearClusters,
    [property: JsonPropertyName("near_clusters_listing_different_patients")] int NearClustersDifferent,
    [property: JsonPropertyName("recrop_pairs")] int RecropPairs,
    [property: JsonPropertyName("mirrored_copy_pairs")] int MirroredPairs,
    [property: JsonPropertyName("recrop_rule")] string RecropRule,
    [property: JsonPropertyName("images_added_by_recrop")] int RecropAdded,
    [property: JsonPropertyName("clusters_with_recrops")] int RecropClusters,
    [property: JsonPropertyName("recrop_clusters_listing_different_patients")] int RecropClustersDifferent,
    [property: JsonPropertyName("copy_clusters_total")] int CopyClusters,
    [property: JsonPropertyName("distinct_photographs")] int DistinctPhotographs,
    [property: JsonPropertyName("unreliable_images")] int Unreliable,
    [property: JsonPropertyName("share_unreliable")] double ShareUnreliable,
    [property: JsonPropertyName("reliable_images_kept")] int ReliableKept);

public record ProtocolVersion(int Version, string Rule,
    [property: JsonPropertyName("reliable_images")] int ReliableImages,
    [property: JsonPropertyName("lockbox_n")] int LockboxN,
    [property: JsonPropertyName("lockbox_auc")] double LockboxAuc,
    [property: JsonPropertyName("lockbox_auc_ci95")] double[] LockboxAucCi95,
    [property: JsonPropertyName("nested_cv_auc")] double NestedCvAuc);

public record DatasetInfo(
    string Name, string Source, string License, string Doi,
    [property: JsonPropertyName("reliable_images")] int Reliable,
    [property: JsonPropertyName("demo_holdout")] int DemoHoldout,
    int Development, int Lockbox, int Anaemic, int Healthy, int Hospitals,
    [property: JsonPropertyName("age_months")] int[] AgeMonths,
    [property: JsonPropertyName("anaemia_definition")] string AnaemiaDefinition);

public record Lockbox(int N, double Auc,
    [property: JsonPropertyName("auc_ci95")] double[] AucCi95,
    [property: JsonPropertyName("at_low_threshold")] Confusion AtLow,
    [property: JsonPropertyName("at_high_threshold")] Confusion AtHigh);

public record NestedCv(
    [property: JsonPropertyName("auc_mean")] double AucMean,
    [property: JsonPropertyName("auc_sd")] double AucSd,
    [property: JsonPropertyName("auc_pooled")] double AucPooled,
    [property: JsonPropertyName("auc_pooled_ci95")] double[] AucPooledCi95);

public record HospitalResult(string Hospital, int N, int Anaemic, double? Auc);

public record Loho([property: JsonPropertyName("pooled_auc")] double PooledAuc,
    [property: JsonPropertyName("per_hospital")] HospitalResult[] PerHospital);

public record Subgroup(string Group, int N, double Auc);

public record HbRegression(double Mae, [property: JsonPropertyName("baseline_mae")] double BaselineMae,
    [property: JsonPropertyName("pearson_r")] double PearsonR);

public record Illuminant(int Kelvin,
    [property: JsonPropertyName("card_white_rgb")] int[] CardWhiteRgb,
    [property: JsonPropertyName("a_star_shift_uncorrected")] double ShiftUncorrected,
    [property: JsonPropertyName("a_star_shift_with_card")] double ShiftWithCard,
    [property: JsonPropertyName("shift_vs_disease_gap_uncorrected")] double TimesDiseaseGap,
    [property: JsonPropertyName("absolute_features_mean_shift_in_sd")] double AbsoluteShiftSd,
    [property: JsonPropertyName("relative_features_mean_shift_in_sd")] double RelativeShiftSd,
    [property: JsonPropertyName("model_prob_mean_abs_change_uncorrected")] double ProbChangeUncorrected,
    [property: JsonPropertyName("model_prob_mean_abs_change_with_card")] double ProbChangeWithCard);

public record LightingSimulation([property: JsonPropertyName("disease_gap_a_star")] double DiseaseGap, Illuminant[] Illuminants);

public record FeatureWeight(string Feature, double Weight);

public record DemoResult(string Id, int Label, double Hb, double P);

public record Metrics(
    Audit Audit,
    [property: JsonPropertyName("protocol_history")] ProtocolVersion[] ProtocolHistory,
    DatasetInfo Dataset,
    [property: JsonPropertyName("feature_set_selection_dev_nested_cv_auc")] Dictionary<string, double> FeatureSetSelection,
    [property: JsonPropertyName("chosen_feature_set")] string ChosenFeatureSet,
    [property: JsonPropertyName("development_nested_cv_auc")] double DevelopmentAuc,
    Lockbox Lockbox,
    [property: JsonPropertyName("nested_cv_all")] NestedCv NestedCvAll,
    [property: JsonPropertyName("operating_points")] Dictionary<string, Confusion> OperatingPoints,
    double[][] Roc,
    [property: JsonPropertyName("single_feature_auc")] Dictionary<string, double> SingleFeatureAuc,
    [property: JsonPropertyName("confound_check")] Dictionary<string, double> ConfoundCheck,
    [property: JsonPropertyName("leave_one_hospital_out")] Loho LeaveOneHospitalOut,
    Subgroup[] Subgroups,
    [property: JsonPropertyName("hb_regression")] HbRegression HbRegression,
    [property: JsonPropertyName("feature_weights")] FeatureWeight[] FeatureWeights,
    [property: JsonPropertyName("lighting_simulation")] LightingSimulation Lighting,
    [property: JsonPropertyName("demo_holdout")] DemoResult[] DemoHoldout,
    /// <summary>[pallor-index cut-off, sensitivity, specificity] for cut-offs 0..100.</summary>
    [property: JsonPropertyName("operating_curve")] double[][] OperatingCurve,
    [property: JsonPropertyName("leakage_demo")] LeakageRow[] LeakageDemo);

public record LeakageRow(string Level, int Images, double Memoriser,
    [property: JsonPropertyName("colour_model")] double ColourModel);

public record BenchmarkData(string[] Features, string[] Hospitals, BenchSample[] Samples);

/// <summary>Loads the model, its validation metrics and the demo samples once per session.</summary>
public sealed class AppData(HttpClient http)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private Task<PallorModel>? _model;
    private Task<Metrics>? _metrics;
    private Task<Sample[]>? _samples;
    private Task<BenchmarkData>? _benchmark;

    private record BenchRow(string Id, int Y, int H, int E, int C, int K, double[] X);
    private record BenchFile(string[] Features, string[] Hospitals, BenchRow[] Rows);

    /// <summary>Colour features of all 710 published images, for the Benchmark Lab (loaded on demand).</summary>
    public Task<BenchmarkData> Benchmark => _benchmark ??= LoadBenchmark();

    private async Task<BenchmarkData> LoadBenchmark()
    {
        var f = (await http.GetFromJsonAsync<BenchFile>("data/cp-anemic-features.json", Options))!;
        return new BenchmarkData(f.Features, f.Hospitals,
            f.Rows.Select(r => new BenchSample(r.Id, r.Y, r.H, r.E, r.C, r.K, r.X)).ToArray());
    }

    public Task<PallorModel> Model => _model ??= LoadModel();
    public Task<Metrics> Metrics => _metrics ??= http.GetFromJsonAsync<Metrics>("model/metrics.json", Options)!;
    public Task<Sample[]> Samples => _samples ??= http.GetFromJsonAsync<Sample[]>("samples/samples.json", Options)!;

    private async Task<PallorModel> LoadModel() => PallorModel.FromJson(await http.GetStringAsync("model/model.json"));
}
