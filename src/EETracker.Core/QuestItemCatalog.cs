using System.Text.Json;

namespace EETracker.Core;

public sealed record QuestInventoryItem(string Id, string Label, string Kind, string SourceHook, string[] RequiredBy);
public sealed record QuestPartCatalogItem(string Id, string Label, string[] PossibleAreas);
public sealed record MapQuestItemCatalog(string DisplayName, QuestInventoryItem[] Items, QuestPartCatalogItem[] Parts);

public static class QuestItemCatalog
{
    public static IReadOnlyDictionary<string, MapQuestItemCatalog> Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "data", "quest-item-catalog.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<string, MapQuestItemCatalog>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in document.RootElement.GetProperty("maps").EnumerateObject())
        {
            var items = map.Value.GetProperty("items").Deserialize<QuestInventoryItem[]>(TelemetryJson.Options) ?? [];
            var parts = map.Value.GetProperty("parts").Deserialize<QuestPartCatalogItem[]>(TelemetryJson.Options) ?? [];
            result[map.Name] = new MapQuestItemCatalog(map.Value.GetProperty("displayName").GetString() ?? map.Name, items, parts);
        }
        return result;
    }
}
