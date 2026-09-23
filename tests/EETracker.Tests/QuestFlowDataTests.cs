using System.Text.Json;
using Xunit;

namespace EETracker.Tests;

public class QuestFlowDataTests
{
    [Fact]
    public void Bo1MainQuestNodesAreUniqueAndEveryPathReferenceResolves()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "bo1-main-quest-flows.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var allIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var map in json.RootElement.GetProperty("maps").EnumerateArray())
        {
            var mapId = map.GetProperty("mapId").GetString()!;
            foreach (var quest in map.GetProperty("quests").EnumerateArray())
            {
                var nodes = quest.GetProperty("nodes").EnumerateArray().ToArray();
                var ids = nodes.Select(n => n.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);
                Assert.Equal(nodes.Length, ids.Count);
                Assert.All(ids, id => Assert.StartsWith($"bo1.{mapId}.", id, StringComparison.Ordinal));
                foreach (var id in ids) Assert.True(allIds.Add(id), $"Duplicate quest step id: {id}");

                foreach (var pathName in new[] { "path", "paths" })
                {
                    if (!quest.TryGetProperty(pathName, out var paths)) continue;
                    var sequences = pathName == "path" ? new[] { paths } : paths.EnumerateObject().Select(p => p.Value);
                    foreach (var sequence in sequences)
                    {
                        var refs = sequence.EnumerateArray().Select(v => v.GetString()!).ToArray();
                        Assert.Equal(refs.Length, refs.Distinct(StringComparer.Ordinal).Count());
                        Assert.All(refs, id => Assert.Contains(id, ids));
                    }
                }
            }
        }

        Assert.Equal(4, json.RootElement.GetProperty("maps").GetArrayLength());
    }
}
