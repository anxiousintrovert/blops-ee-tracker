using System.Text.Json;

namespace EETracker.Core;

/// <summary>Reduces an ordered telemetry stream against the reviewed, map-scoped flow catalog.</summary>
public sealed class QuestEngine
{
    private readonly IReadOnlyList<MapFlow> _maps;
    private readonly HashSet<string> _completed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _observed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _sideEggSteps = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int Progress, int Maximum)> _progress = new(StringComparer.Ordinal);
    private readonly Dictionary<int, TransitProfileState> _transitProfiles = new();
    private readonly Dictionary<string, Bo2ProfileState> _bo2Profiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PlayerInventoryState> _playerInventories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QuestPartState> _questParts = new(StringComparer.Ordinal);
    private string _lastSignal = "No quest signal received";
    private string _map = "Waiting for game";
    private int? _round, _players;
    private bool? _power;
    private GameVariant _variant = GameVariant.Unknown;
    private bool _connected;
    private bool _sessionEnded;
    private bool _connectionInterrupted;
    private int? _pressureSecondsRemaining;
    private bool? _pressureTimerActive;
    private int? _lunaLettersCollected;
    private bool _lunaSequenceReset;
    private int[]? _soulTankFill, _soulTankMaxFill;
    private string _samanthaColors = "";
    private string _richtofenCue = "";
    private DateTimeOffset? _lastSamanthaColor;
    private DateTimeOffset? _lastEvent;
    private string? _selectedPath;
    private bool _transitQuestStarted;
    private bool _transitMaxisActionStarted;
    private bool _hasCurrentSession;

    public QuestEngine(string? flowPath = null) => _maps = QuestFlowCatalog.LoadAll(flowPath);
    public CompanionState State => BuildState();

    public void Apply(TelemetryEvent e)
    {
        if (e.SchemaVersion != 1) throw new InvalidDataException($"Unsupported telemetry schema {e.SchemaVersion}.");
        if (e.Type == "session_started")
        {
            _connected = true; _hasCurrentSession = true; _map = "Waiting for game"; _round = null; _players = null; _power = null;
            _variant = GameVariant.Unknown; _completed.Clear(); _observed.Clear(); _sideEggSteps.Clear(); _lastSignal = "No quest signal received";
            _selectedPath = null;
            _progress.Clear();
            _transitProfiles.Clear();
            _bo2Profiles.Clear();
            _playerInventories.Clear();
            _questParts.Clear();
            _sessionEnded = false; _connectionInterrupted = false; _pressureSecondsRemaining = null; _pressureTimerActive = null;
            _transitQuestStarted = _observed.Contains("bo2.transit.started");
            _transitMaxisActionStarted = false;
            _lunaLettersCollected = null; _lunaSequenceReset = false;
            _soulTankFill = null; _soulTankMaxFill = null; _samanthaColors = ""; _richtofenCue = ""; _lastSamanthaColor = null;
        }
        if (e.Type == "session_ended") { _connected = false; _sessionEnded = true; _connectionInterrupted = false; _pressureSecondsRemaining = null; _pressureTimerActive = null; }
        if (e.Type == "transport_status")
        {
            _connected = e.SignalValue == "connected";
            _connectionInterrupted = !_connected;
            if (_connected) _sessionEnded = false;
            if (!_connected) { _pressureSecondsRemaining = null; _pressureTimerActive = null; }
        }
        if (e.Type == "pressure_timer")
        {
            _pressureSecondsRemaining = e.SecondsRemaining;
            _pressureTimerActive = e.SignalValue == "running";
        }
        if (e.Type == "luna_progress" && e.LettersCollected is >= 0 and <= 4)
        {
            _lunaSequenceReset = e.LettersCollected == 0 && _lunaLettersCollected > 0;
            _lunaLettersCollected = e.LettersCollected;
        }
        if (e.Type == "soul_tank_progress" && e.SoulTankFill is not null && e.SoulTankMaxFill is not null)
        {
            _soulTankFill = e.SoulTankFill.ToArray();
            _soulTankMaxFill = e.SoulTankMaxFill.ToArray();
        }
        if (e.Type == "quest_progress" && e.Signal is { Length: > 0 } progressKey && e.Progress is >= 0 && e.ProgressMax is > 0)
            _progress[progressKey] = (Math.Min(e.Progress.Value, e.ProgressMax.Value), e.ProgressMax.Value);
        if (e.Type == "side_egg_step" && !string.IsNullOrWhiteSpace(e.Map) && !string.IsNullOrWhiteSpace(e.EggId) && e.StepIndex is >= 0)
            _sideEggSteps.Add($"{e.Map}|{e.EggId}|{e.StepIndex.Value}");
        if (e.Type == "player_state" && e.Game == "bo2" && e.PlayerSlot is { } slot)
        {
            var profileMap = NormalizeMap(e.ProfileMap ?? e.Map ?? _map);
            _bo2Profiles[$"{profileMap}:{slot}"] = new Bo2ProfileState(profileMap, slot, e.LastCompletedSide, e.RichtofenCompletionCount, e.MaxisCompletionCount, e.NavcardAppliedCount);
            if (profileMap == "TranZit") _transitProfiles[slot] = new TransitProfileState(slot, e.LastCompletedSide, e.RichtofenCompletionCount, e.MaxisCompletionCount, e.NavcardAppliedCount);
            if (_map == "TranZit")
            {
                if (e.LastCompletedSide == 1) _completed.Add("bo2.transit.tower_of_babble.richtofen.emp_lights");
                if (e.LastCompletedSide == 2) _completed.Add("bo2.transit.tower_of_babble.maxis.screecher_lights");
            }
        }
        if (e.Type == "samantha_color" && e.SignalValue is { Length: > 0 } color)
        {
            if (_lastSamanthaColor is { } previous && e.TimestampUtc - previous > TimeSpan.FromSeconds(2)) _samanthaColors = "";
            _samanthaColors = string.IsNullOrEmpty(_samanthaColors) ? color : _samanthaColors + "  ·  " + color;
            _lastSamanthaColor = e.TimestampUtc;
        }
        if (e.Type == "richtofen_cue" && e.SignalValue is { Length: > 0 } cue) _richtofenCue = cue;
        if (e.Map is not null)
        {
            var nextMap = NormalizeMap(e.Map);
            if (!string.Equals(_map, nextMap, StringComparison.OrdinalIgnoreCase)) _selectedPath = null;
            if (!string.Equals(_map, nextMap, StringComparison.OrdinalIgnoreCase) && nextMap != "TranZit") _transitQuestStarted = false;
            _map = nextMap;
        }
        if (e.Round is not null) _round = e.Round;
        if (e.PlayerCount is not null) _players = e.PlayerCount;
        if (e.PowerOn is not null) _power = e.PowerOn;
        if (_map == "TranZit" && e.PowerOn == true)
            _completed.Add("bo2.transit.tower_of_babble.power");
        if (_map == "TranZit" && e.Type == "quest_progress"
            && e.Signal == "bo2.transit.maxis.turbines_at_pylon"
            && e.Progress is { } turbineCount)
        {
            if (turbineCount > 0)
            {
                _transitMaxisActionStarted = true;
                if (_selectedPath is null) _selectedPath = "maxis";
            }
            if (e.ProgressMax is { } turbineMaximum && turbineCount >= turbineMaximum)
                _completed.Add("bo2.transit.tower_of_babble.maxis.turbines");
        }
        if (e.Type == "player_inventory" && e.Game is { Length: > 0 } game && e.PlayerSlot is { } inventorySlot)
        {
            var inventoryMap = NormalizeMap(e.Map ?? _map);
            var inventoryKey = $"{game}:{inventoryMap}:{inventorySlot}";
            var items = (e.InventoryItems ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _playerInventories[inventoryKey] = new PlayerInventoryState(game, inventoryMap, inventorySlot, e.PlayerName, items);
        }
        if (e.Type == "quest_part" && e.Game is { Length: > 0 } partGame && !string.IsNullOrWhiteSpace(e.PartId) && !string.IsNullOrWhiteSpace(e.PartState))
        {
            var partMap = NormalizeMap(e.Map ?? _map);
            var prefix = $"{partGame}:{partMap}:{e.PartId}:";
            int? partSlot = e.PlayerSlot;
            if (e.PartState == "built")
            {
                foreach (var oldKey in _questParts.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
                    _questParts.Remove(oldKey);
                partSlot = null;
            }
            else if (e.PartState == "dropped" && partSlot is { } droppingSlot)
                _questParts.Remove(prefix + droppingSlot);
            else if (e.PartState == "carried" && partSlot is { } carryingSlot)
                _questParts.Remove(prefix + carryingSlot);
            else if (e.PartState == "dropped")
                foreach (var oldKey in _questParts.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
                    _questParts.Remove(oldKey);
            var ownerKey = e.PartState == "carried" && partSlot is { } carriedSlot ? carriedSlot.ToString() : "team";
            var partKey = prefix + ownerKey;
            _questParts[partKey] = new QuestPartState(partGame, partMap, e.PartId, e.PartState, e.PartState == "carried" ? partSlot : null, e.PartState == "carried" ? e.PlayerName : null, e.PartAreas, e.PartLabel, e.PartOrigin);
        }
        if (e.VariantEvidence is { } evidence) ApplyVariantEvidence(evidence);
        if (e.Type == "session_started") { _selectedPath = null; _hasCurrentSession = true; _transitQuestStarted = _observed.Contains("bo2.transit.started"); }
        if (e.Type == "quest_path_selected" && !string.IsNullOrWhiteSpace(e.SignalValue))
        {
            if (_map != "TranZit" || e.SignalValue != "maxis" || _transitMaxisActionStarted)
                _selectedPath = e.SignalValue;
        }
        if (e.Type == "quest_signal" && !string.IsNullOrWhiteSpace(e.Signal))
        {
            if (e.Signal == "bo2.transit.started")
            {
                _transitQuestStarted = true;
                _completed.Add("bo2.transit.tower_of_babble.setup");
            }
            if (e.Signal.StartsWith("bo2.mob.", StringComparison.Ordinal))
                _observed.Add(e.Signal);
            if (e.Signal.StartsWith("coast.dial.", StringComparison.Ordinal) && e.Signal.EndsWith(".incorrect", StringComparison.Ordinal))
                _observed.Remove(e.Signal[..^"incorrect".Length] + "correct");
            else
                _observed.Add(e.Signal);
            _lastSignal = e.Signal == "ascension.monkey_button_interaction"
                ? "Monkey button interaction observed · step completion not confirmed"
                : e.Signal;
            var raw = e.Signal.StartsWith("stock.success.", StringComparison.Ordinal) ? e.Signal["stock.success.".Length..] : e.Signal;
            foreach (var node in CurrentQuest()?.Nodes ?? Array.Empty<FlowNode>())
                if (node.SourceFlag == raw) _completed.Add(node.Id);
            if (raw == "passkey_confirmed") { _lunaLettersCollected = 4; _lunaSequenceReset = false; }
        }
        _lastEvent = e.TimestampUtc;
    }

    private void ApplyVariantEvidence(VariantEvidence evidence)
    {
        if (evidence.Kind == EvidenceKind.ExplicitVariant)
            _variant = evidence.Value switch { "vanilla" => GameVariant.Vanilla, "any_player_ee" => GameVariant.AnyPlayerEe, "any_player_ee_sr" => GameVariant.AnyPlayerEeSr, _ => GameVariant.Unknown };
        else if (evidence.Kind == EvidenceKind.ModLoaded)
            _variant = evidence.Value switch { "any_player_ee" => GameVariant.AnyPlayerEe, "any_player_ee_sr" => GameVariant.AnyPlayerEeSr, _ => _variant };
    }

    private CompanionState BuildState()
    {
        var map = _maps.FirstOrDefault(x => x.Aliases.Contains(_map, StringComparer.OrdinalIgnoreCase));
        var quest = map?.Quests.FirstOrDefault();
        var isCallOfTheDead = map?.Aliases.Contains("zombie_coast", StringComparer.OrdinalIgnoreCase) == true;
        var isTransit = map?.Aliases.Contains("zm_transit", StringComparer.OrdinalIgnoreCase) == true;
        var detectedCoastPath = isCallOfTheDead ? _players switch { 1 => "stand_in", > 1 => "ensemble_cast", _ => null } : null;
        var waitingForCoastPlayers = isCallOfTheDead && detectedCoastPath is null;
        var isMobEnding = map?.Aliases.Contains("zm_prison", StringComparer.OrdinalIgnoreCase) == true;
        var detectedMobEnding = isMobEnding
            ? (_observed.Contains("bo2.mob.break_cycle.complete") || _observed.Contains("bo2.mob.continue_cycle.complete")) ? _selectedPath : null
            : null;
        var pathName = isCallOfTheDead ? detectedCoastPath : isMobEnding ? detectedMobEnding : _selectedPath ?? (_variant switch {
            GameVariant.Vanilla => "vanilla", GameVariant.AnyPlayerEe => "any_player_ee",
            GameVariant.AnyPlayerEeSr => "any_player_ee_sr", _ => null
        });
        var selectedIds = quest?.Paths is { Count: > 0 } paths
            ? (pathName is not null && paths.TryGetValue(pathName, out var exact) ? exact : null)
            : quest?.DefaultPath;
        if (isTransit && !_hasCurrentSession) _transitQuestStarted = _observed.Contains("bo2.transit.started");
        var branchChoiceRequired = false;
        string[] commonPrefix = Array.Empty<string>();
        if (quest?.Paths is { Count: > 0 } branchPaths && selectedIds is null)
        {
            var sequences = branchPaths.Values.ToArray();
            var prefix = new List<string>();
            for (var i = 0; sequences.Length > 1 && sequences.All(s => s.Length > i && s[i] == sequences[0][i]); i++)
                prefix.Add(sequences[0][i]);
            commonPrefix = prefix.ToArray();
            selectedIds = commonPrefix;
            var forkReached = commonPrefix.All(_completed.Contains);
            branchChoiceRequired = !isCallOfTheDead && !isMobEnding && selectedIds.Length < branchPaths.Values.Max(v => v.Length) && forkReached && (!isTransit || _transitQuestStarted);
        }
        var nodeLookup = (quest?.Nodes ?? Array.Empty<FlowNode>()).ToDictionary(n => n.Id, StringComparer.Ordinal);
        var ordered = selectedIds is null
            ? quest?.Nodes ?? Array.Empty<FlowNode>()
            : selectedIds.Where(nodeLookup.ContainsKey).Select(id => nodeLookup[id]).ToArray();
        var branchOptions = branchChoiceRequired && quest?.Paths is { Count: > 0 } availablePaths
            ? availablePaths.Select(path => {
                var first = path.Value.Skip(commonPrefix.Length).FirstOrDefault();
                var node = first is null ? null : nodeLookup.GetValueOrDefault(first);
                var name = path.Key switch { "maxis" => "MAXIS", "richtofen" => "RICHTOFEN", _ => path.Key.Replace('_', ' ').ToUpperInvariant() };
                var choiceLabel = path.Key switch { "richtofen" => "A · RICHTOFEN", "maxis" => "B · MAXIS", _ => name };
                var instruction = node?.Instruction ?? "Route details have not been researched.";
                if (isTransit)
                    instruction = path.Key == "richtofen"
                        ? "Turn map power on to start Richtofen's route. The tracker follows that stock route signal; then build the Jet Gun and heat the pylon."
                        : "Keep map power off and place the first Turbine beneath the cornfield pylon to begin Maxis's route. The tracker selects Maxis from that action and follows both Turbines.";
                return new QuestBranchOption(path.Key, name, choiceLabel, node?.Title ?? "Route details unavailable", instruction);
            }).OrderBy(option => option.Key == "richtofen" ? 0 : option.Key == "maxis" ? 1 : 2).ToArray()
            : Array.Empty<QuestBranchOption>();
        var current = ordered.FirstOrDefault(n => !_completed.Contains(n.Id));
        var previous = current is null ? null : ordered.TakeWhile(n => n.Id != current.Id).LastOrDefault();
        var signalObserved = _observed.Contains("ascension.monkey_button_interaction");
        var progression = ordered.Select(n => new QuestStep(n.Id, n.Title,
            n.Id == current?.Id ? n.Instruction : n.Instruction,
            _completed.Contains(n.Id) ? "Complete" : n.Id == current?.Id ? "Current" : "Upcoming",
            n.Detection)).ToArray();
        var trackers = current is null ? Array.Empty<StepTrackerState>() : BuildTrackers(current);
        var preparation = ResolveRequirements(quest, pathName);
        var objective = current?.Title ?? (quest is null ? "Choose a supported map" : branchChoiceRequired ? "Choose a side" : waitingForCoastPlayers ? "Detecting Call of the Dead players" : "Quest complete");
        var instruction = current?.Instruction ?? (quest is null ? "Map flow unavailable for this session." : branchChoiceRequired ? "Both route openings are shown below. The first branch-specific quest signal will identify which route the game is following." : waitingForCoastPlayers ? "The game will select Stand-In or Ensemble Cast when the player count is detected." : "All tracked quest steps have completion signals.");
        if (isMobEnding && detectedMobEnding is null && current is null)
        {
            objective = "Reach the final showdown";
            instruction = "The shared setup is complete. Finish the showdown; the game will reveal whether the cycle continues or is broken.";
        }
        if (map?.DisplayName == "Ascension" && current?.Id.EndsWith(".monkey_buttons", StringComparison.Ordinal) == true)
            instruction = BuildMonkeyButtonInstruction();
        if (map?.DisplayName == "Ascension" && current?.Id.EndsWith(".pressure_plate", StringComparison.Ordinal) == true)
            instruction = BuildPressurePlateInstruction();
        if (map?.DisplayName == "Ascension" && current?.Id.EndsWith(".luna_passkey", StringComparison.Ordinal) == true)
            instruction = BuildLunaInstruction();
        if (signalObserved && current?.SourceFlag == "switches_synced")
            instruction += " A monkey-button interaction was observed, but switches_synced has not been confirmed.";
        var next = current is null && waitingForCoastPlayers ? "Waiting for player count." :
            current is null && branchChoiceRequired ? "Waiting for the game to confirm its route." :
            current is null ? "No further steps in this flow." :
            current == ordered.FirstOrDefault() ? "Complete the current objective to reveal the next step." :
            $"After this: {ordered.SkipWhile(n => n.Id != current.Id).Skip(1).FirstOrDefault()?.Title ?? "Quest complete"}";
        return new CompanionState {
            Map = map?.DisplayName ?? _map, Round = _round, PlayerCount = _players, PowerOn = _power, Variant = _variant,
            Connected = _connected, SessionEnded = _sessionEnded, ConnectionInterrupted = _connectionInterrupted,
            PressureSecondsRemaining = _pressureSecondsRemaining,
            PressureTimerActive = _pressureTimerActive,
            LunaLettersCollected = _lunaLettersCollected, LunaSequenceReset = _lunaSequenceReset,
            SoulTankFill = _soulTankFill, SoulTankMaxFill = _soulTankMaxFill,
            SamanthaColors = _samanthaColors, RichtofenCue = _richtofenCue,
            CurrentTrackers = trackers,
            CurrentObjective = objective, Instruction = instruction, Preparation = preparation,
            NextStep = next, Progression = progression, LastEventUtc = _lastEvent,
            QuestName = quest?.DisplayName ?? "Main quest", StepCount = ordered.Count,
            CompletedCount = _completed.Count(id => ordered.Any(n => n.Id == id)),
            PreviousStep = previous?.Title ?? "First step", RecentSignal = _lastSignal,
            RequiresPathChoice = branchChoiceRequired,
            SelectedPath = pathName,
            BranchOptions = branchOptions,
            TransitProfiles = _transitProfiles.Values.OrderBy(profile => profile.PlayerSlot).ToArray(),
            Bo2Profiles = _bo2Profiles.Values.OrderBy(profile => profile.Map, StringComparer.Ordinal).ThenBy(profile => profile.PlayerSlot).ToArray(),
            SideEggProgress = _sideEggSteps.Select(key => key.Split('|')).Where(parts => parts.Length == 3 && int.TryParse(parts[2], out _)).Select(parts => new SideEggStepProgress(parts[0], parts[1], int.Parse(parts[2]))).ToArray(),
            PlayerInventories = _playerInventories.Values.Where(item => string.Equals(item.Map, _map, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.PlayerSlot).ToArray(),
            QuestParts = _questParts.Values.Where(item => string.Equals(item.Map, _map, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.PartId, StringComparer.Ordinal).ToArray()
        };
    }

    private IReadOnlyList<PreparationRequirement> ResolveRequirements(QuestFlow? quest, string? pathName)
    {
        if (quest is null) return Array.Empty<PreparationRequirement>();
        var result = new List<PreparationRequirement>();
        if (quest.Requirements.TryGetValue("shared", out var sharedItems))
        {
            result.AddRange(sharedItems.Select((text, index) => new PreparationRequirement($"shared-{index}", "shared", text)));
            if (pathName is not null && quest.Requirements.TryGetValue(pathName, out var selectedItems))
                result.AddRange(selectedItems.Select((text, index) => new PreparationRequirement($"{pathName}-{index}", pathName, text)));
            return result;
        }
        var key = _variant switch { GameVariant.Vanilla => "vanilla", GameVariant.AnyPlayerEe => "any_player_ee", GameVariant.AnyPlayerEeSr => "any_player_ee_sr", _ => "unknown" };
        if (quest.Requirements.TryGetValue(key, out var items))
            result.AddRange(items.Where(text => !text.Equals("Solo play is supported", StringComparison.OrdinalIgnoreCase))
                .Select((text, index) => new PreparationRequirement($"{key}-{index}", "shared", text)));
        else if (quest.Requirements.TryGetValue("unknown", out var baseline))
            result.AddRange(baseline.Select((text, index) => new PreparationRequirement($"unknown-{index}", "shared", text)));
        return result;
    }

    private StepTrackerState[] BuildTrackers(FlowNode current)
    {
        return current.Trackers.Select(tracker => {
            var checkpoints = tracker.Checkpoints.Select(checkpoint => new StepCheckpointState(
                checkpoint.Label,
                _observed.Contains("stock.success." + checkpoint.Signal) || _observed.Contains(checkpoint.Signal))).ToArray();
            var hasProgress = _progress.TryGetValue(tracker.Signal, out var progress);
            var maximum = hasProgress ? progress.Maximum : tracker.Maximum ?? checkpoints.Length;
            var value = hasProgress ? progress.Progress : checkpoints.Count(x => x.Complete);
            var summary = hasProgress || checkpoints.Length > 0
                ? $"{value} / {maximum}"
                : "Waiting for game confirmation";
            return new StepTrackerState(tracker.Title, summary, value, Math.Max(1, maximum), checkpoints);
        }).ToArray();
    }

    private string BuildMonkeyButtonInstruction()
    {
        const string locations = "Juggernog (opposite the machine), PhD Flopper (left wall), Speed Cola (across the doorway), and Stamin-Up (left wall).";
        if (_players is not > 0)
            return $"During a Space Monkey round, press the perk buttons: {locations}";

        var playerLabel = _players == 1 ? "1 player" : $"{_players} players";
        if (_variant is GameVariant.Vanilla or GameVariant.AnyPlayerEe)
            return $"During a Space Monkey round, with {playerLabel}, press {_players} different perk button{(_players == 1 ? "" : "s")}: {locations} Coordinate the presses before the timer runs out.";

        return $"During a Space Monkey round, with {playerLabel}, press the perk buttons: {locations} The required number depends on the active variant.";
    }

    private string BuildPressurePlateInstruction()
    {
        if (_players is > 0)
        {
            var countText = _players == 1 ? "Solo: stay inside" : $"All {_players} players stay inside";
            return $"{countText} the pressure area by the rocket pad for two minutes. Leaving resets the timer.";
        }
        return "Have every player stay inside the pressure area by the rocket pad for two minutes. Leaving resets the timer.";
    }

    private string BuildLunaInstruction()
    {
        const string routes = "Call the lander for L → U → N → A: Spawn to Stamin-Up, Stamin-Up to Spawn, Spawn to Speed Cola, then Speed Cola to Stamin-Up.";
        if (_players == 1)
            return routes + " Solo: call it to each destination pad; it collects the letter as it passes. A missed letter resets the sequence.";
        if (_players is > 1)
            return routes + $" With {_players} players, keep a player aboard to collect each letter. A missed letter resets the sequence.";
        return routes + " Make sure each letter is collected before landing. A missed letter resets the sequence.";
    }

    private QuestFlow? CurrentQuest() => _maps.FirstOrDefault(x => x.Aliases.Contains(_map, StringComparer.OrdinalIgnoreCase))?.Quests.FirstOrDefault();
    private static string NormalizeMap(string raw) => raw switch {
        "zombie_cosmodrome" => "Ascension", "zombie_coast" => "Call of the Dead", "zombie_temple" => "Shangri-La", "zombie_moon" => "Moon",
        "zm_transit" or "bo2_transit" => "TranZit",
        "zm_highrise" or "bo2_die_rise" => "Die Rise",
        "zm_buried" or "bo2_buried" => "Buried",
        "zm_prison" or "bo2_mob" => "Mob of the Dead",
        "zm_tomb" or "bo2_origins" => "Origins",
        "ascension" => "Ascension", "call_of_the_dead" => "Call of the Dead", "shangri_la" => "Shangri-La", "moon" => "Moon", _ => raw
    };
}

public sealed record FlowNode(string Id, string Title, string Instruction, string? SourceFlag, string Detection, IReadOnlyList<StepProgressTracker> Trackers, IReadOnlyList<FlowChecklist> Checklists);
public sealed record FlowChecklist(string Title, IReadOnlyList<FlowChecklistItem> Items);
public sealed record FlowChecklistItem(string Id, string Label, string Location);
public sealed record QuestFlow(string DisplayName, IReadOnlyList<FlowNode> Nodes, Dictionary<string, string[]> Requirements, string[]? DefaultPath, Dictionary<string, string[]> Paths);
public sealed record MapFlow(string DisplayName, string[] Aliases, IReadOnlyList<QuestFlow> Quests);

public static class QuestFlowCatalog
{
    public static IReadOnlyList<MapFlow> Load(string? path = null)
        => LoadSingle(path ?? Path.Combine(AppContext.BaseDirectory, "data", "bo1-main-quest-flows.json"));

    public static IReadOnlyList<MapFlow> LoadAll(string? path = null)
    {
        if (path is not null) return LoadSingle(path);
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        return LoadSingle(Path.Combine(dataDirectory, "bo1-main-quest-flows.json"))
            .Concat(LoadSingle(Path.Combine(dataDirectory, "bo2-main-quest-flows.json"))).ToArray();
    }

    private static IReadOnlyList<MapFlow> LoadSingle(string path)
    {
        if (!File.Exists(path)) return Array.Empty<MapFlow>();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("maps").EnumerateArray().Select(m => {
            var code = m.GetProperty("mapCode").GetString()!;
            var aliases = new[] { m.GetProperty("mapId").GetString()!, code, m.GetProperty("displayName").GetString()! };
            var quests = m.GetProperty("quests").EnumerateArray().Select(q => {
                var nodes = q.GetProperty("nodes").EnumerateArray().Select(n => {
                    var trackers = new List<StepProgressTracker>();
                    if (n.TryGetProperty("trackers", out var trackerArray))
                        foreach (var tracker in trackerArray.EnumerateArray())
                        {
                            var checkpoints = tracker.TryGetProperty("checkpoints", out var items)
                                ? items.EnumerateArray().Select(item => new StepCheckpoint(item.GetProperty("label").GetString()!, item.GetProperty("signal").GetString()!)).ToArray()
                                : Array.Empty<StepCheckpoint>();
                            trackers.Add(new StepProgressTracker(
                                tracker.GetProperty("title").GetString()!, tracker.GetProperty("signal").GetString()!,
                                tracker.TryGetProperty("maximum", out var max) && max.ValueKind == JsonValueKind.Number ? max.GetInt32() : null,
                                checkpoints));
                        }
                    var checklists = new List<FlowChecklist>();
                    if (n.TryGetProperty("checklists", out var checklistArray))
                        foreach (var checklist in checklistArray.EnumerateArray())
                        {
                            var checklistTitle = checklist.GetProperty("title").GetString()!;
                            checklists.Add(new FlowChecklist(
                                checklistTitle,
                                checklist.GetProperty("items").EnumerateArray().Select(item => new FlowChecklistItem(
                                    checklistTitle + "|" + item.GetProperty("id").GetString()!, item.GetProperty("label").GetString()!,
                                    item.GetProperty("location").GetString()!)).ToArray()));
                        }
                    return new FlowNode(n.GetProperty("id").GetString()!, n.GetProperty("title").GetString()!,
                        n.GetProperty("instruction").GetString()!, n.TryGetProperty("sourceFlag", out var flag) ? flag.GetString() : null,
                        n.TryGetProperty("detection", out var detection) ? detection.GetString() ?? "manual" : "manual", trackers, checklists);
                }).ToArray();
                var requirements = new Dictionary<string, string[]>(StringComparer.Ordinal);
                if (q.TryGetProperty("requirements", out var req))
                    foreach (var prop in req.EnumerateObject())
                        requirements[prop.Name] = prop.Value.ValueKind == JsonValueKind.Array
                            ? prop.Value.EnumerateArray().Select(v => v.GetString() ?? "").ToArray()
                            : new[] { prop.Value.GetString() ?? "Requirements unknown." };
                var defaultPath = q.TryGetProperty("path", out var singlePath) && singlePath.ValueKind == JsonValueKind.Array
                    ? singlePath.EnumerateArray().Select(v => v.GetString()!).ToArray() : null;
                var paths = new Dictionary<string, string[]>(StringComparer.Ordinal);
                if (q.TryGetProperty("paths", out var pathOptions))
                    foreach (var p in pathOptions.EnumerateObject())
                        paths[p.Name] = p.Value.EnumerateArray().Select(v => v.GetString()!).ToArray();
                return new QuestFlow(q.GetProperty("displayName").GetString()!, nodes, requirements, defaultPath, paths);
            }).ToArray();
            return new MapFlow(m.GetProperty("displayName").GetString()!, aliases, quests);
        }).ToArray();
    }
}
