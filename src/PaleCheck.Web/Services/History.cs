using System.Globalization;
using System.Text;
using System.Text.Json;
using PaleCheck.Core;

namespace PaleCheck.Web.Services;

public record HistoryEntry(
    string Id,
    DateTimeOffset When,
    int PallorIndex,
    PallorBand Band,
    double Probability,
    double AStar,
    double LStar,
    bool UsedCard,
    double? LabHb = null,
    string? LabDate = null,
    int Photos = 1);

/// <summary>
/// Measurements saved on this device only (browser storage). Nothing is uploaded.
/// Stores numbers, never photos.
/// </summary>
public sealed class History(Js js)
{
    private const string Key = "palecheck.history.v1";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<List<HistoryEntry>> LoadAsync()
    {
        var json = await js.InvokeAsync<string?>("storageGet", Key);
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<HistoryEntry>>(json, Options) ?? []; }
        catch (JsonException) { return []; }
    }

    public async Task<bool> SaveAsync(List<HistoryEntry> entries) =>
        await js.InvokeAsync<bool>("storageSet", Key, JsonSerializer.Serialize(entries, Options));

    public async Task<bool> AddAsync(HistoryEntry entry)
    {
        var all = await LoadAsync();
        all.Insert(0, entry);
        return await SaveAsync(all);
    }

    public async Task ClearAsync() => await js.InvokeVoidAsync("storageRemove", Key);

    /// <summary>CSV for the research protocol: numbers only, no photos, no names.</summary>
    public static string ToCsv(IEnumerable<HistoryEntry> entries)
    {
        var sb = new StringBuilder("date,pallor_index,band,probability,a_star,L_star,used_card,photos,lab_hb_g_dl,lab_date\n");
        foreach (var e in entries.OrderBy(e => e.When))
        {
            sb.Append(e.When.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
              .Append(e.PallorIndex).Append(',')
              .Append(e.Band).Append(',')
              .Append(e.Probability.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
              .Append(e.AStar.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .Append(e.LStar.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .Append(e.UsedCard ? "yes" : "no").Append(',')
              .Append(e.Photos).Append(',')
              .Append(e.LabHb?.ToString("0.0", CultureInfo.InvariantCulture) ?? "").Append(',')
              .Append(e.LabDate ?? "").Append('\n');
        }
        return sb.ToString();
    }
}
