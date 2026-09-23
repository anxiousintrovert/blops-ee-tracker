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
    public int? Round { get; init; }
    public int? PlayerCount { get; init; }
    public bool? PowerOn { get; init; }
    public string? Signal { get; init; }
    public string? SignalValue { get; init; }
    public int? SecondsRemaining { get; init; }
    public int? LettersCollected { get; init; }
    public int? Progress { get; init; }
    public int? ProgressMax { get; init; }
    public int[]? SoulTankFill { get; init; }
    public int[]? SoulTankMaxFill { get; init; }
    public VariantEvidence? VariantEvidence { get; init; }
    public string Source { get; init; } = "unknown";
}

public sealed record QuestStep(string Id, string Title, string Instruction, string Status, string Detection);
public sealed record StepCheckpoint(string Label, string Signal);
public sealed record StepProgressTracker(string Title, string Signal, int? Maximum, IReadOnlyList<StepCheckpoint> Checkpoints);
public sealed record StepTrackerState(string Title, string Summary, int Progress, int Maximum, IReadOnlyList<StepCheckpointState> Checkpoints);
public sealed record StepCheckpointState(string Label, bool Complete);

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
    public string CurrentObjective { get; init; } = "Waiting for Ascension telemetry";
    public string Instruction { get; init; } = "Start a replay or connect the GSC log source.";
    public string Preparation { get; init; } = "Power status not yet observed";
    public string NextStep { get; init; } = "Waiting for quest signals";
    public IReadOnlyList<QuestStep> Progression { get; init; } = Array.Empty<QuestStep>();
    public DateTimeOffset? LastEventUtc { get; init; }
    public string QuestName { get; init; } = "Main quest";
    public string PreviousStep { get; init; } = "—";
    public int StepCount { get; init; }
    public int CompletedCount { get; init; }
    public string RecentSignal { get; init; } = "No quest signal received";
    public bool RequiresPathChoice { get; init; }
}

public static class TelemetryJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
    public static TelemetryEvent? Parse(string line)
    {
        // T5 GSC can emit compact signal records. The file transport supplies receive-time/session metadata.
        var node = JsonNode.Parse(line) as JsonObject ?? throw new InvalidDataException("Telemetry line must be a JSON object.");
        node.TryAdd("schemaVersion", JsonValue.Create(1));
        node.TryAdd("timestampUtc", JsonValue.Create(DateTimeOffset.UtcNow));
        node.TryAdd("sessionId", JsonValue.Create("gsc-file"));
        node.TryAdd("source", JsonValue.Create("gsc-scriptdata"));
        return node.Deserialize<TelemetryEvent>(Options);
    }
}
