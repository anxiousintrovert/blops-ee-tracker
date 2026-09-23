using EETracker.Core;
using Xunit;

namespace EETracker.Tests;

public class QuestEngineTests
{
    private static TelemetryEvent Event(string type, string? map = null, int? round = null, int? players = null, bool? power = null, VariantEvidence? evidence = null, string? signal = null, string? value = null) => new()
    { TimestampUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"), SessionId = "test", Type = type, Map = map, Round = round, PlayerCount = players, PowerOn = power, VariantEvidence = evidence, Signal = signal, SignalValue = value };

    [Fact]
    public void UnknownVariantRemainsUnknownWithoutEvidence()
    {
        var engine = new QuestEngine(); engine.Apply(Event("snapshot", "zombie_cosmodrome", 12, 2, true));
        Assert.Equal("Ascension", engine.State.Map); Assert.Equal(12, engine.State.Round); Assert.Equal(2, engine.State.PlayerCount); Assert.Equal(true, engine.State.PowerOn); Assert.Equal(GameVariant.Unknown, engine.State.Variant);
    }

    [Theory]
    [InlineData("vanilla", GameVariant.Vanilla)]
    [InlineData("any_player_ee", GameVariant.AnyPlayerEe)]
    [InlineData("any_player_ee_sr", GameVariant.AnyPlayerEeSr)]
    [InlineData("unrecognized", GameVariant.Unknown)]
    public void ExplicitVariantEvidenceIsMapped(string raw, GameVariant expected)
    {
        var engine = new QuestEngine(); engine.Apply(Event("variant", evidence: new(EvidenceKind.ExplicitVariant, raw)));
        Assert.Equal(expected, engine.State.Variant);
    }

    [Theory]
    [InlineData("any_player_ee", GameVariant.AnyPlayerEe)]
    [InlineData("any_player_ee_sr", GameVariant.AnyPlayerEeSr)]
    public void PositivelyIdentifiedModVariantIsMapped(string raw, GameVariant expected)
    {
        var engine = new QuestEngine();
        engine.Apply(Event("mod_loaded", evidence: new(EvidenceKind.ModLoaded, raw)));
        Assert.Equal(expected, engine.State.Variant);
    }

    [Fact]
    public void ConfirmedAscensionQuestFlagCompletesTheMatchingStep()
    {
        var engine = new QuestEngine();
        engine.Apply(Event("snapshot", "zombie_cosmodrome", 1, 1, false));
        engine.Apply(Event("quest_signal", signal: "stock.success.target_teleported"));

        Assert.Equal("Complete", engine.State.Progression[0].Status);
        Assert.Equal("Activate the Casimir terminal", engine.State.CurrentObjective);
    }

    [Fact]
    public void MonkeyButtonSignalDrivesQuestObjective()
    {
        var engine = new QuestEngine();
        engine.Apply(Event("snapshot", "zombie_cosmodrome", 1, 1, true));
        engine.Apply(Event("quest_signal", signal: "ascension.monkey_button_interaction"));
        Assert.Equal("Teleport the Gersh Device", engine.State.CurrentObjective);
        Assert.Contains("step completion not confirmed", engine.State.RecentSignal);
        Assert.Equal(0, engine.State.CompletedCount);
    }

    [Theory]
    [InlineData("zombie_cosmodrome", "Ascension")]
    [InlineData("zombie_coast", "Call of the Dead")]
    [InlineData("zombie_temple", "Shangri-La")]
    [InlineData("zombie_moon", "Moon")]
    public void GameplayProjectionUsesScriptFlowOrder(string mapId, string mapName)
    {
        var engine = new QuestEngine();
        engine.Apply(Event("snapshot", mapId, 1, mapId == "zombie_coast" ? 2 : 1, true));
        var state = engine.State;
        var catalog = QuestFlowCatalog.Load();
        var map = Assert.Single(catalog, x => x.DisplayName == mapName);
        var quest = Assert.Single(map.Quests);
        var expectedIds = quest.Paths is { Count: > 0 }
            ? quest.Paths[mapId == "zombie_coast" ? "ensemble_cast" : "vanilla"]
            : quest.DefaultPath ?? quest.Nodes.Select(x => x.Id).ToArray();

        Assert.Equal(expectedIds, state.Progression.Select(x => x.Id));
        Assert.Equal(expectedIds.Length, state.StepCount);
        Assert.Equal(expectedIds[0], state.Progression[0].Id);
        Assert.Equal("Current", state.Progression[0].Status);
        Assert.Equal(1, state.Progression.Count(x => x.Status == "Current"));
        Assert.Equal(expectedIds.Length - 1, state.Progression.Count(x => x.Status == "Upcoming"));
        Assert.Equal(quest.Nodes.Single(x => x.Id == expectedIds[0]).Title, state.CurrentObjective);
        Assert.Contains("Complete the current objective", state.NextStep);
    }

    [Fact]
    public async Task ReplayFeedsSameReducerEndToEnd()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "ascension-session.jsonl");
        if (!File.Exists(path)) path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "samples", "ascension-session.jsonl"));
        var engine = new QuestEngine(); await using var replay = new JsonlReplaySource(path); replay.EventReceived += engine.Apply; await replay.StartAsync(CancellationToken.None);
        Assert.Equal("Ascension", engine.State.Map); Assert.Equal(12, engine.State.Round); Assert.Equal(2, engine.State.PlayerCount); Assert.Equal(GameVariant.AnyPlayerEe, engine.State.Variant); Assert.Equal("Activate the Casimir terminal", engine.State.CurrentObjective); Assert.Contains("Monkey button interaction observed", engine.State.RecentSignal);
    }
}
