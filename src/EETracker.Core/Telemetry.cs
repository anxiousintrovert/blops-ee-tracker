using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EETracker.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameVariant { Unknown, Vanilla, AnyPlayerEe, AnyPlayerEeSr }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EvidenceKind { ExplicitVariant, ModLoaded, DvarSnapshot, GameplaySignal }

public sealed record VariantEvidence(EvidenceKind Kind, string Value);

public sealed record TelemetryEvent
{
    public int SchemaVersion { get; init; } = 1;
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string SessionId { get; init; }
    public required string Type { get; init; }
    public string? Map { get; init; }
    public string? ProfileMap { get; init; }
    public int? Round { get; init; }
    public int? PlayerCount { get; init; }
    public bool? PowerOn { get; init; }
    public string? Signal { get; init; }
    public string? SignalValue { get; init; }
    public string? EggId { get; init; }
    public int? StepIndex { get; init; }
    public int? SecondsRemaining { get; init; }
    public int? LettersCollected { get; init; }
    public int? Progress { get; init; }
    public int? ProgressMax { get; init; }
    public int[]? SoulTankFill { get; init; }
    public int[]? SoulTankMaxFill { get; init; }
    public VariantEvidence? VariantEvidence { get; init; }
    public string? Game { get; init; }
    public int? PlayerSlot { get; init; }
    public string? PlayerName { get; init; }
    public string? InventoryItems { get; init; }
    public string? PartId { get; init; }
    public string? PartLabel { get; init; }
    public string? PartState { get; init; }
    public string? PartAreas { get; init; }
    public string? PartOrigin { get; init; }
    public int? LastCompletedSide { get; init; }
    public int? RichtofenStage1Count { get; init; }
    public int? RichtofenStage2Count { get; init; }
    public int? RichtofenStage3Count { get; init; }
    public int? RichtofenCompletionCount { get; init; }
    public int? MaxisStage1Count { get; init; }
    public int? MaxisStage2Count { get; init; }
    public int? MaxisStage3Count { get; init; }
    public int? MaxisCompletionCount { get; init; }
    public int? NavcardAppliedCount { get; init; }
    public int? NavcardTableBuiltCount { get; init; }
    public bool? NavcardHeld { get; init; }
    public int? Bank { get; init; }
    public int? TileId { get; init; }
    public int? PeerBank { get; init; }
    public int? PeerTileId { get; init; }
    public string? TileState { get; init; }
    public string? DoorId { get; init; }
    public string? DoorState { get; init; }
    public int? DoorCost { get; init; }
    public string Source { get; init; } = "unknown";
}

public sealed record QuestStep(string Id, string Title, string Instruction, string Status, string Detection);
public sealed record QuestBranchOption(string Key, string Name, string ChoiceLabel, string FirstObjective, string Instruction);
public sealed record StepCheckpoint(string Label, string Signal);
public sealed record StepProgressTracker(string Title, string Signal, int? Maximum, IReadOnlyList<StepCheckpoint> Checkpoints);
public sealed record FlowPlayerCountGuidance(string Mod, string? Path, int MinPlayers, int? MaxPlayers, string Instruction);
public sealed record StepTrackerState(string Title, string Summary, int Progress, int Maximum, IReadOnlyList<StepCheckpointState> Checkpoints);
public sealed record StepCheckpointState(string Label, bool Complete);
public sealed record SideEggStepProgress(string Map, string EggId, int StepIndex);
public sealed record PlayerInventoryState(string Game, string Map, int PlayerSlot, string? PlayerName, IReadOnlyList<string> Items);
public sealed record QuestPartState(string Game, string Map, string PartId, string State, int? PlayerSlot, string? PlayerName, string? PossibleAreas, string? Label, string? Origin);
public sealed record PreparationRequirement(string Id, string Scope, string Text);
public sealed record DoorGuideState(string DoorId, string Label, int? Cost, string GateType, bool? IsOpen, bool RequiredOpen);

public sealed record CompanionState
{
    public string Map { get; init; } = "Waiting for game";
    public int? Round { get; init; }
    public int? PlayerCount { get; init; }
    public bool? PowerOn { get; init; }
    public GameVariant Variant { get; init; } = GameVariant.Unknown;
    public bool Connected { get; init; }
    public bool SessionEnded { get; init; }
    public bool ConnectionInterrupted { get; init; }
    public int? PressureSecondsRemaining { get; init; }
    public bool? PressureTimerActive { get; init; }
    public int? LunaLettersCollected { get; init; }
    public bool LunaSequenceReset { get; init; }
    public IReadOnlyList<int>? SoulTankFill { get; init; }
    public IReadOnlyList<int>? SoulTankMaxFill { get; init; }
    public string SamanthaColors { get; init; } = "";
    public string RichtofenCue { get; init; } = "";
    public IReadOnlyList<StepTrackerState> CurrentTrackers { get; init; } = Array.Empty<StepTrackerState>();
    public IReadOnlyList<TempleTileCellState> TempleTileBanks { get; init; } = Array.Empty<TempleTileCellState>();
    public IReadOnlyList<string> DieRiseTileSequence { get; init; } = Array.Empty<string>();
    public int? DieRiseTileProgress { get; init; }
    public int? DieRiseFloorProgress { get; init; }
    public string CurrentObjective { get; init; } = "Waiting for Ascension telemetry";
    public string Instruction { get; init; } = "Start a replay or connect the GSC log source.";
    public IReadOnlyList<PreparationRequirement> Preparation { get; init; } = Array.Empty<PreparationRequirement>();
    public string NextStep { get; init; } = "Waiting for quest signals";
    public IReadOnlyList<QuestStep> Progression { get; init; } = Array.Empty<QuestStep>();
    public DateTimeOffset? LastEventUtc { get; init; }
    public string QuestName { get; init; } = "Main quest";
    public string PreviousStep { get; init; } = "—";
    public int StepCount { get; init; }
    public int CompletedCount { get; init; }
    public string RecentSignal { get; init; } = "No quest signal received";
    public bool RequiresPathChoice { get; init; }
    public string? SelectedPath { get; init; }
    public IReadOnlyList<QuestBranchOption> BranchOptions { get; init; } = Array.Empty<QuestBranchOption>();
    public IReadOnlyList<TransitProfileState> TransitProfiles { get; init; } = Array.Empty<TransitProfileState>();
    public IReadOnlyList<Bo2ProfileState> Bo2Profiles { get; init; } = Array.Empty<Bo2ProfileState>();
    public IReadOnlyList<SideEggStepProgress> SideEggProgress { get; init; } = Array.Empty<SideEggStepProgress>();
    public IReadOnlyList<PlayerInventoryState> PlayerInventories { get; init; } = Array.Empty<PlayerInventoryState>();
    public IReadOnlyList<QuestPartState> QuestParts { get; init; } = Array.Empty<QuestPartState>();
    public IReadOnlyList<DoorGuideState> Doors { get; init; } = Array.Empty<DoorGuideState>();
}

public sealed record TransitProfileState(int PlayerSlot, int? LastCompletedSide, int? RichtofenCompletionCount, int? MaxisCompletionCount, int? NavcardAppliedCount);
public sealed record Bo2ProfileState(string Map, int PlayerSlot, int? LastCompletedSide, int? RichtofenCompletionCount, int? MaxisCompletionCount, int? NavcardAppliedCount, bool? NavcardHeld = null, int? NavcardTableBuiltCount = null);
public sealed record TempleTileCellState(int Bank, int TileId, bool Selected, bool Matched);

public static class TelemetryJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
    public static TelemetryEvent? Parse(string line)
    {
        // T5 GSC can emit compact signal records. The file transport supplies receive-time/session metadata.
        // Windows PowerShell's UTF-8 output may prefix the first JSONL record with a BOM.
        var normalizedLine = line.TrimStart('\uFEFF', '\u200B');
        if (string.IsNullOrWhiteSpace(normalizedLine)) return null;
        var node = JsonNode.Parse(normalizedLine) as JsonObject ?? throw new InvalidDataException("Telemetry line must be a JSON object.");
        node.TryAdd("schemaVersion", JsonValue.Create(1));
        node.TryAdd("timestampUtc", JsonValue.Create(DateTimeOffset.UtcNow));
        node.TryAdd("sessionId", JsonValue.Create("gsc-file"));
        node.TryAdd("source", JsonValue.Create("gsc-scriptdata"));
        return node.Deserialize<TelemetryEvent>(Options);
    }
}
