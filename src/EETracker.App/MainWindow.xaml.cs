using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Text.Json;
using System.ComponentModel;
using EETracker.Core;
using Microsoft.Win32;

namespace EETracker.App;

public partial class MainWindow : Window
{
    private QuestEngine _engine = new();
    private CancellationTokenSource? _run;
    private string _lastEvent = "No activity yet";
    private string _sourceName = "No source";
    private string _activeGame = "bo1";
    private Process? _t6Collector;
    private readonly string _checklistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EETracker", "full-quest-checks.json");
    private Dictionary<string, bool> _checklistState = new(StringComparer.Ordinal);
    private string _fullQuestRoute = "";
    private bool _loadingWalkthrough;
    private bool _loadingPreparation;
    private readonly IReadOnlyList<WalkthroughMapChoice> _walkthroughMaps = WalkthroughMaps();
    private readonly IReadOnlyDictionary<string, MapQuestItemCatalog> _questItemCatalog = QuestItemCatalog.Load();

    public MainWindow()
    {
        InitializeComponent();
        try { if (File.Exists(_checklistPath)) _checklistState = JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(_checklistPath)) ?? new(StringComparer.Ordinal); }
        catch (JsonException) { _checklistState = new(StringComparer.Ordinal); }
        FullQuestMapBox.ItemsSource = _walkthroughMaps.Where(x => x.GameId == _activeGame).ToArray();
        SideEggMapBox.ItemsSource = SideEggMaps(_activeGame);
        SelectWalkthroughMapForCurrentState();
        Closed += (_, _) => StopT6Collector();
        PathBox.Text = Path.Combine(AppContext.BaseDirectory, "samples", "ascension-session.jsonl");
        Refresh();
        SizeChanged += (_, _) => UpdateTrackerLayout();
        Loaded += async (_, _) =>
        {
            if (HasRecentBo2Telemetry())
            {
                _activeGame = "bo2";
                Bo1Tab.Style = (Style)FindResource("RailButton");
                Bo2Tab.Style = (Style)FindResource("RailActiveButton");
            }
            var args = Environment.GetCommandLineArgs();
            var previewIndex = Array.IndexOf(args, "--preview");
            var captureIndex = Array.IndexOf(args, "--capture-layout");
            if (previewIndex >= 0 && previewIndex + 1 < args.Length)
            {
                PathBox.Text = Path.GetFullPath(args[previewIndex + 1]);
                var previewFirstLine = File.ReadLines(PathBox.Text).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
                var previewEvent = previewFirstLine is null ? null : TelemetryJson.Parse(previewFirstLine);
                if (previewEvent?.Game == "bo2" || previewEvent?.Map?.StartsWith("zm_", StringComparison.Ordinal) == true)
                {
                    _activeGame = "bo2";
                    Bo1Tab.Style = (Style)FindResource("RailButton");
                    Bo2Tab.Style = (Style)FindResource("RailActiveButton");
                }
                SetPage(MissionPage);
                await StartSource(new JsonlReplaySource(PathBox.Text, TimeSpan.FromMilliseconds(180)), "Sample preview");
                if (captureIndex >= 0 && captureIndex + 1 < args.Length)
                {
                    await Task.Delay(700);
                    SaveWindowCapture(Path.GetFullPath(args[captureIndex + 1]));
                    Close();
                }
                return;
            }
            if (captureIndex >= 0 && captureIndex + 1 < args.Length)
            {
                await StartSource(new JsonlReplaySource(PathBox.Text, TimeSpan.FromMilliseconds(700)), "Ascension sample replay");
                await Task.Delay(350);
                SaveWindowCapture(args[captureIndex + 1]);
                Close();
                return;
            }
            await ConnectSelectedGameAsync();
        };
    }

    private static bool HasRecentBo2Telemetry()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Plutonium", "storage", "t6", "raw", "scriptdata", "ee-tracker-bo2.jsonl");
        try
        {
            if (!File.Exists(path) || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromMinutes(5)) return false;
            var line = File.ReadLines(path).LastOrDefault(item => !string.IsNullOrWhiteSpace(item));
            var telemetry = line is null ? null : TelemetryJson.Parse(line);
            return telemetry?.Game == "bo2" && !string.IsNullOrWhiteSpace(telemetry.Map);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private async Task StartSource(ITelemetrySource source, string label)
    {
        _run?.Cancel();
        _run?.Dispose();
        var run = new CancellationTokenSource();
        _run = run;
        _engine = new QuestEngine();
        _sourceName = label;
        _lastEvent = "Waiting for game activity";
        source.EventReceived += e => Dispatcher.Invoke(() => { if (!ReferenceEquals(_run, run)) return; _engine.Apply(e); ApplySideEggEvent(e); _lastEvent = $"{e.TimestampUtc:HH:mm:ss}  {FriendlyActivity(e)}"; SettingsStatus.Text = $"Receiving {label.ToLowerInvariant()} data"; Refresh(); });
        SettingsStatus.Text = label.Contains("game session", StringComparison.OrdinalIgnoreCase) ? $"Waiting for game data at {PathBox.Text}" : $"Source: {label}";
        Refresh();
        try { await source.StartAsync(run.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Dispatcher.Invoke(() => { if (!ReferenceEquals(_run, run)) return; SettingsStatus.Text = $"Could not open this session: {ex.Message}"; _lastEvent = "Session could not be opened"; Refresh(); }); }
        finally { await source.DisposeAsync(); }
    }

    private void Refresh()
    {
        var s = _engine.State;
        var sessionMap = _walkthroughMaps.FirstOrDefault(item => string.Equals(item.Label, s.Map, StringComparison.OrdinalIgnoreCase));
        if (sessionMap is not null && (_activeGame != sessionMap.GameId || (FullQuestMapBox.SelectedItem as WalkthroughMapChoice)?.Label != sessionMap.Label))
            SelectWalkthroughMapForCurrentState();
        MapTitle.Text = s.Map.ToUpperInvariant();
        QuestTitle.Text = s.QuestName.ToUpperInvariant();
        ApplyMapBanner(_activeGame, s.Map);
        RoundText.Text = s.Round?.ToString() ?? "—";
        PlayersText.Text = s.PlayerCount?.ToString() ?? "—";
        RouteText.Text = s.RequiresPathChoice ? "ROUTE PENDING" : s.Map == "TranZit" ? s.SelectedPath?.ToUpperInvariant() ?? "ROUTE UNCONFIRMED" : s.Map == "Call of the Dead" ? s.PlayerCount switch { 1 => "STAND-IN", > 1 => "ENSEMBLE CAST", _ => "DETECTING" } : "AUTOMATIC";
        PowerText.Text = s.PowerOn switch { true => "ON", false => "OFF", _ => "—" };
        var mapCode = s.Map switch { "Ascension" => "zombie_cosmodrome", "Call of the Dead" => "zombie_coast", "Shangri-La" => "zombie_temple", "Moon" => "zombie_moon", "TranZit" => "zm_transit", "Die Rise" => "zm_highrise", "Buried" => "zm_buried", "Mob of the Dead" => "zm_prison", "Origins" => "zm_tomb", _ => "" };
        _questItemCatalog.TryGetValue(mapCode, out var itemCatalog);
        var itemLabels = itemCatalog?.Items.ToDictionary(item => item.Id, item => item.Label, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inventoryRows = s.PlayerInventories.Select(player =>
        {
            var labels = player.Items.Where(itemLabels.ContainsKey).Select(item => itemLabels[item]).Distinct().ToArray();
            return $"P{player.PlayerSlot + 1}: {(labels.Length == 0 ? "No tracked quest items" : string.Join(", ", labels))}";
        })
            .Concat(s.QuestParts.Select(part => $"{itemCatalog?.Parts.FirstOrDefault(item => item.Id == part.PartId)?.Label ?? part.Label ?? part.PartId}: {part.State}{(part.PlayerSlot is { } carrier ? $" · P{carrier + 1}" : " · team")}{(!string.IsNullOrWhiteSpace(part.PossibleAreas) ? $" · {part.PossibleAreas}" : "")}{(!string.IsNullOrWhiteSpace(part.Origin) ? $" · {part.Origin}" : "")}"))
            .Concat(itemCatalog?.Parts.Where(part => part.PossibleAreas.Length > 0 && s.QuestParts.All(observed => observed.PartId != part.Id)).Select(part => $"{part.Label}: not confirmed · possible {string.Join(" / ", part.PossibleAreas)}") ?? [])
            .ToArray();
        InventoryText.Text = inventoryRows.Length == 0 ? "Waiting for player inventory telemetry" : string.Join("\n", inventoryRows);
        ConnectionText.Text = s.Connected ? "CONNECTED" : s.SessionEnded ? "GAME ENDED" : s.ConnectionInterrupted ? "NO GAME SIGNAL" : "WAITING";
        ConnectionDot.Fill = s.Connected ? (Brush)FindResource("AccentOlive") : (Brush)FindResource("AccentAmber");
        ObjectiveText.Text = s.CurrentObjective;
        InstructionText.Text = s.Instruction;
        _loadingPreparation = true;
        var objectiveTracker = s.CurrentTrackers.FirstOrDefault();
        TrackerProgressPanel.Visibility = objectiveTracker is null ? Visibility.Collapsed : Visibility.Visible;
        if (objectiveTracker is not null)
        {
            var hasCount = objectiveTracker.Maximum > 1;
            var isCoastDialTracker = s.Map == "Call of the Dead" && objectiveTracker.Title == "LIGHTHOUSE DIALS SET CORRECTLY";
            QuestRingContainer.Visibility = hasCount ? Visibility.Visible : Visibility.Collapsed;
            QuestRingText.Text = hasCount ? $"{objectiveTracker.Progress} / {objectiveTracker.Maximum}" : objectiveTracker.Summary;
            QuestRingLabel.Text = isCoastDialTracker ? "DIALS CORRECT" : hasCount ? objectiveTracker.Title : "OBJECTIVE CHECKS";
            UpdateQuestRing(hasCount ? (double)objectiveTracker.Progress / objectiveTracker.Maximum : 0);
        }
        else QuestRingContainer.Visibility = Visibility.Collapsed;
        var onPressureStep = s.Map == "Ascension" && s.Progression.Any(step => step.Status == "Current" && step.Id.EndsWith(".pressure_plate", StringComparison.Ordinal));
        PressureTimerText.Visibility = onPressureStep && s.PressureSecondsRemaining is not null ? Visibility.Visible : Visibility.Collapsed;
        if (PressureTimerText.Visibility == Visibility.Visible)
        {
            TrackerProgressPanel.Visibility = Visibility.Visible;
            QuestRingContainer.Visibility = Visibility.Visible;
            var secondsRemaining = Math.Max(0, s.PressureSecondsRemaining!.Value);
            QuestRingText.Text = TimeSpan.FromSeconds(secondsRemaining).ToString(@"mm\:ss");
            QuestRingLabel.Text = s.PressureTimerActive == true ? "REMAINING" : "RESET";
            UpdateQuestRing(1d - secondsRemaining / 120d);
        }
        if (s.PressureSecondsRemaining is { } seconds)
            PressureTimerText.Text = s.PressureTimerActive == true
                ? $"PRESSURE PAD · {TimeSpan.FromSeconds(seconds):mm\\:ss} REMAINING"
                : "PRESSURE PAD · RESET — GET EVERYONE ON THE PAD";
        var onLunaStep = s.Map == "Ascension" && s.Progression.Any(step => step.Status == "Current" && step.Id.EndsWith(".luna_passkey", StringComparison.Ordinal));
        LunaProgressText.Visibility = onLunaStep && s.LunaLettersCollected is not null ? Visibility.Visible : Visibility.Collapsed;
        if (s.LunaLettersCollected is { } letters)
        {
            var sequence = "LUNA".Select((letter, index) => $"{letter} {(index < letters ? "✓" : "○")}");
            LunaProgressText.Text = "LETTERS  " + string.Join("   ", sequence) + (s.LunaSequenceReset ? "   ·   Sequence reset — start with L" : "");
        }
        var onSamanthaStep = s.Map == "Moon" && s.Progression.Any(step => step.Status == "Current" && (step.Id.EndsWith(".samantha_says_initial", StringComparison.Ordinal) || step.Id.EndsWith(".maxis_says", StringComparison.Ordinal)));
        SamanthaPromptText.Visibility = onSamanthaStep && !string.IsNullOrEmpty(s.SamanthaColors) ? Visibility.Visible : Visibility.Collapsed;
        SamanthaPromptText.Inlines.Clear();
        if (!string.IsNullOrEmpty(s.SamanthaColors))
        {
            SamanthaPromptText.Inlines.Add(new Run("SAMANTHA SAYS · ") { Foreground = (Brush)FindResource("TextSoft") });
            foreach (var color in s.SamanthaColors.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var brush = color.Trim().ToUpperInvariant() switch
                {
                    "RED" => Brushes.OrangeRed,
                    "GREEN" => Brushes.LightGreen,
                    "BLUE" => Brushes.DeepSkyBlue,
                    "YELLOW" => Brushes.Gold,
                    _ => (Brush)FindResource("AccentOlive")
                };
                SamanthaPromptText.Inlines.Add(new Run($"● {color}   ") { Foreground = brush, FontWeight = FontWeights.Bold });
            }
        }
        var currentId = s.Progression.FirstOrDefault(step => step.Status == "Current")?.Id ?? "";
        var firstSoulTube = s.Map == "Moon" && currentId.EndsWith(".open_mpd", StringComparison.Ordinal);
        var allSoulTubes = s.Map == "Moon" && currentId.EndsWith(".four_soul_tubes", StringComparison.Ordinal);
        SoulTanksPanel.Visibility = (firstSoulTube || allSoulTubes) && s.SoulTankFill is not null && s.SoulTankMaxFill is not null ? Visibility.Visible : Visibility.Collapsed;
        SoulTank2Panel.Visibility = allSoulTubes ? Visibility.Visible : Visibility.Collapsed;
        SoulTank3Panel.Visibility = allSoulTubes ? Visibility.Visible : Visibility.Collapsed;
        SoulTank4Panel.Visibility = allSoulTubes ? Visibility.Visible : Visibility.Collapsed;
        UpdateSoulTank(0, SoulTank1Bar, SoulTank1Text, s);
        UpdateSoulTank(1, SoulTank2Bar, SoulTank2Text, s);
        UpdateSoulTank(2, SoulTank3Bar, SoulTank3Text, s);
        UpdateSoulTank(3, SoulTank4Bar, SoulTank4Text, s);
        RichtofenCueText.Visibility = s.Map == "Moon" && !string.IsNullOrEmpty(s.RichtofenCue) ? Visibility.Visible : Visibility.Collapsed;
        if (!string.IsNullOrEmpty(s.RichtofenCue)) RichtofenCueText.Text = "RICHTOFEN'S CUE · " + s.RichtofenCue;
        StepTrackers.ItemsSource = s.CurrentTrackers.Select(tracker => new StepTrackerViewModel(tracker)).ToArray();
        StepTrackers.Visibility = s.CurrentTrackers.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        var currentFlowStep = s.Progression.FirstOrDefault(step => step.Status == "Current");
        var objectiveChecklist = s.Map == "Origins" && currentFlowStep?.Id == "bo2.origins.setup"
            ? QuestFlowCatalog.LoadAll().FirstOrDefault(map => map.DisplayName == "Origins")?.Quests.FirstOrDefault()?.Nodes.FirstOrDefault(node => node.Id == currentFlowStep.Id)?.Checklists
                .Select(group => new ObjectiveChecklistViewModel(group.Title, group.Items.Select(item => new ObjectiveChecklistItemViewModel(
                    item.Label, item.Location, _checklistState.GetValueOrDefault($"bo2|Origins|Little Lost Girl|{currentFlowStep.Id}|item|{item.Id}"))).ToArray())).ToArray()
            : null;
        ObjectiveChecklistItems.ItemsSource = objectiveChecklist;
        ObjectiveChecklistItems.Visibility = objectiveChecklist is { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        UpdateTrackerLayout();
        PreparationItems.ItemsSource = s.Preparation.Select(item => new PreparationItemViewModel(
            PrepKey(_activeGame, s.Map, s.QuestName, item.Scope, s.SelectedPath, item.Id), item.Text,
            _checklistState.GetValueOrDefault(PrepKey(_activeGame, s.Map, s.QuestName, item.Scope, s.SelectedPath, item.Id)))).ToArray();
        _loadingPreparation = false;
        NextText.Text = s.NextStep.StartsWith("After this: ", StringComparison.Ordinal)
            ? s.NextStep["After this: ".Length..] : s.NextStep;
        var currentStep = Array.FindIndex(s.Progression.ToArray(), step => step.Status == "Current");
        var nextStep = currentStep >= 0 && currentStep + 1 < s.Progression.Count ? s.Progression[currentStep + 1] : null;
        NextInstructionText.Visibility = nextStep is null ? Visibility.Collapsed : Visibility.Visible;
        NextInstructionText.Text = nextStep?.Instruction ?? "";
        PreviousText.Text = $"Previous: {s.PreviousStep}";
        PathChoicePanel.Visibility = s.Map == "Call of the Dead" && s.RequiresPathChoice ? Visibility.Visible : Visibility.Collapsed;
        BranchOptionsPanel.Visibility = s.BranchOptions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        BranchOptionsItems.ItemsSource = s.BranchOptions;
        RuntimeText.Text = _activeGame == "bo1" ? "PLUTONIUM T5" : "PLUTONIUM T6";
        ConnectGameButton.Content = _activeGame == "bo1" ? "CONNECT TO BO1 GAME" : "CONNECT TO BO2 GAME";
        var columns = ActualWidth < 1120 ? 2 : 3;
        ProgressList.ItemsSource = s.Progression.Select((step, index) => new ProgressStepViewModel(step, index + 1, columns)).ToArray();
        ProgressPanel.Visibility = s.StepCount == 0 ? Visibility.Collapsed : Visibility.Visible;
        ProgressText.Text = $"{s.CompletedCount} / {s.StepCount} STAGES COMPLETE";
        QuestProgressBar.Value = s.StepCount == 0 ? 0 : 100d * s.CompletedCount / s.StepCount;
        var currentIndex = Array.FindIndex(s.Progression.ToArray(), x => x.Status == "Current");
        ObjectiveCounter.Text = s.StepCount == 0 ? "NO FLOW LOADED" :
            s.RequiresPathChoice && currentIndex < 0 ? "SELECT ROUTE" : $"STEP {Math.Max(1, currentIndex + 1):00} / {s.StepCount:00}";
        DetectionText.Text = "QUEST STATE";
        RecentSignalText.Text = s.RecentSignal;
        DiagnosticsText.Text = $"Map: {s.Map}\nRound: {s.Round?.ToString() ?? "Unknown"}\nPlayers: {s.PlayerCount?.ToString() ?? "Unknown"}\nPower: {s.PowerOn?.ToString() ?? "Unknown"}\nConnection: {(s.Connected ? "Connected" : s.SessionEnded ? "Ended" : s.ConnectionInterrupted ? "Signal lost" : "Waiting")}\nSession source: {_sourceName}\nRecent activity: {_lastEvent}";
        if (s.Bo2Profiles.Count > 0)
        {
            var profiles = string.Join("\n", s.Bo2Profiles.Select(profile => {
                var side = profile.LastCompletedSide switch { 1 => "Richtofen", 2 => "Maxis", _ => "Unknown" };
                var navcard = profile.NavcardAppliedCount switch { > 0 => "applied", 0 => "not applied", _ => "unknown" };
                return $"{profile.Map} · Player {profile.PlayerSlot + 1}: saved side {side}; completions R {profile.RichtofenCompletionCount ?? 0} / M {profile.MaxisCompletionCount ?? 0}; map NAVcard {navcard}";
            }));
            DiagnosticsText.Text += "\n\nBO2 PROFILE STATE (current lobby; slot identity may change)\n" + profiles;
        }
    }

    private void UpdateTrackerLayout()
    {
        var stacked = ActualWidth < 1350;
        var centerOnly = QuestRingContainer.Visibility != Visibility.Visible;
        System.Windows.Controls.Grid.SetRow(StepTrackers, stacked && !centerOnly ? 1 : 0);
        System.Windows.Controls.Grid.SetColumn(StepTrackers, stacked || centerOnly ? 0 : 2);
        System.Windows.Controls.Grid.SetColumnSpan(StepTrackers, stacked || centerOnly ? 3 : 1);
        StepTrackers.HorizontalAlignment = stacked || centerOnly ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        StepTrackers.MaxWidth = stacked || centerOnly ? 460 : double.PositiveInfinity;
        StepTrackers.Margin = stacked && !centerOnly ? new Thickness(0, 14, 0, 0)
            : centerOnly ? new Thickness(0) : new Thickness(18, 0, 0, 0);
    }

    private void UpdateQuestRing(double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        if (progress <= 0)
        {
            QuestRingProgress.Data = Geometry.Empty;
            return;
        }
        if (progress >= 0.999)
        {
            QuestRingProgress.Data = new EllipseGeometry(new Rect(6, 6, 144, 144));
            return;
        }
        const double radius = 72;
        var center = new Point(78, 78);
        var start = new Point(center.X, center.Y - radius);
        var angle = (-90 + 360 * progress) * Math.PI / 180;
        var end = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, progress > 0.5, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        QuestRingProgress.Data = geometry;
    }

    private void ApplyMapBanner(string game, string map)
    {
        BannerArtwork.Visibility = Visibility.Visible;
        var asset = (game, map) switch
        {
            ("bo1", "Call of the Dead") => "call-of-the-dead-banner.png",
            ("bo1", "Shangri-La") => "shangri-la-banner.png",
            ("bo1", "Moon") => "moon-banner.png",
            ("bo2", "TranZit") => "tranzit-banner.png",
            ("bo2", "Die Rise") => "die-rise-banner.png",
            ("bo2", "Buried") => "buried-banner.png",
            ("bo2", "Mob of the Dead") => "mob-of-the-dead-banner.png",
            ("bo2", "Origins") => "origins-banner.png",
            _ => "ascension-banner.png"
        };
        BannerArtwork.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/{asset}", UriKind.Absolute));
    }

    private static void UpdateSoulTank(int index, System.Windows.Controls.ProgressBar bar, System.Windows.Controls.TextBlock text, CompanionState state)
    {
        if (state.SoulTankFill is not { } fills || state.SoulTankMaxFill is not { } maximums || index >= fills.Count || index >= maximums.Count || maximums[index] <= 0)
        {
            bar.Value = 0;
            text.Text = "Awaiting game state";
            return;
        }
        var fill = Math.Clamp(fills[index], 0, maximums[index]);
        bar.Value = 100d * fill / maximums[index];
        text.Text = $"{fill} / {maximums[index]}";
    }

    private void ShowMain(object sender, RoutedEventArgs e) => SetPage(MissionPage);
    private void ShowFullQuest(object sender, RoutedEventArgs e) { SelectWalkthroughMapForCurrentState(); RefreshFullQuest(); SetPage(FullQuestPage); }
    private void ShowSideEasterEggs(object sender, RoutedEventArgs e) { SelectWalkthroughMapForCurrentState(); RefreshSideEggs(); SetPage(SideEasterEggsPage); }
    private void ShowSettings(object sender, RoutedEventArgs e) => SetPage(SettingsPage);
    private void ShowDiagnostics(object sender, RoutedEventArgs e) => SetPage(DiagnosticsPage);
    private void SetPage(UIElement page) { MissionPage.Visibility = Visibility.Collapsed; FullQuestPage.Visibility = Visibility.Collapsed; SideEasterEggsPage.Visibility = Visibility.Collapsed; SettingsPage.Visibility = Visibility.Collapsed; DiagnosticsPage.Visibility = Visibility.Collapsed; page.Visibility = Visibility.Visible; }

    private static IReadOnlyList<WalkthroughMapChoice> WalkthroughMaps() => QuestFlowCatalog.LoadAll()
        .Select((map, index) => new WalkthroughMapChoice(map.DisplayName, index,
            map.Aliases.Any(alias => alias.StartsWith("bo2_", StringComparison.Ordinal)) ? "bo2" : "bo1",
            map.Aliases.FirstOrDefault(alias => alias.StartsWith("bo2_", StringComparison.Ordinal) || !alias.Contains('.')) ?? map.DisplayName)).ToArray();

    private void SelectWalkthroughMapForCurrentState()
    {
        var state = _engine.State;
        var game = state.Map is "TranZit" or "Die Rise" or "Buried" or "Mob of the Dead" or "Origins" ? "bo2" : _activeGame;
        var maps = _walkthroughMaps;
        var choice = maps.FirstOrDefault(item => item.GameId == game && string.Equals(item.Label, state.Map, StringComparison.OrdinalIgnoreCase))
            ?? maps.FirstOrDefault(item => item.GameId == game);
        if (choice is null) return;

        if (_activeGame != game)
        {
            _activeGame = game;
            Bo1Tab.Style = (Style)FindResource(game == "bo1" ? "RailActiveButton" : "RailButton");
            Bo2Tab.Style = (Style)FindResource(game == "bo2" ? "RailActiveButton" : "RailButton");
        }
        FullQuestMapBox.ItemsSource = maps.Where(item => item.GameId == game).ToArray();
        SideEggMapBox.ItemsSource = SideEggMaps(game);
        FullQuestMapBox.SelectedItem = FullQuestMapBox.ItemsSource is IEnumerable<WalkthroughMapChoice> full && full.Any(item => item.MapId == choice.MapId)
            ? full.First(item => item.MapId == choice.MapId)
            : choice;
        SideEggMapBox.SelectedItem = SideEggMapBox.ItemsSource is IEnumerable<WalkthroughMapChoice> side && side.Any(item => item.Label == choice.Label)
            ? side.First(item => item.Label == choice.Label)
            : SideEggMaps(game).FirstOrDefault(item => item.Label == choice.Label);
    }

    private static IReadOnlyList<WalkthroughMapChoice> SideEggMaps(string game)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "side-easter-egg-catalog.json");
        if (!File.Exists(path)) return Array.Empty<WalkthroughMapChoice>();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("games").EnumerateArray()
            .Where(g => g.GetProperty("gameId").GetString() == game)
            .SelectMany(g => g.GetProperty("maps").EnumerateArray())
            .Select(m => new WalkthroughMapChoice(m.GetProperty("displayName").GetString()!, -1, game, m.GetProperty("mapId").GetString()!)).ToArray();
    }

    private (MapFlow? Map, QuestFlow? Quest) SelectedWalkthrough()
    {
        if (FullQuestMapBox.SelectedItem is not WalkthroughMapChoice choice) return (null, null);
        var map = choice.Index < 0 ? null : QuestFlowCatalog.LoadAll().ElementAtOrDefault(choice.Index);
        return (map, map?.Quests.FirstOrDefault());
    }

    private void FullQuestMapChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (FullQuestMapBox.IsLoaded) { _fullQuestRoute = ""; RefreshFullQuest(); }
    }

    private void SelectFullQuestRoute(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string route }) { _fullQuestRoute = route; RefreshFullQuest(); }
    }

    private void RefreshFullQuest()
    {
        if (FullQuestMapBox?.SelectedItem is not WalkthroughMapChoice choice) return;
        var map = choice.Index < 0 ? null : QuestFlowCatalog.LoadAll().ElementAtOrDefault(choice.Index);
        var quest = map?.Quests.FirstOrDefault();
        if (map is null || quest is null) return;
        var routes = quest.Paths ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
        var routeNames = routes.Keys.ToArray();
        if (routeNames.Length > 0 && !routeNames.Contains(_fullQuestRoute, StringComparer.Ordinal)) _fullQuestRoute = routeNames[0];
        var routeIds = routeNames.Length > 0 ? routes[_fullQuestRoute] : quest.DefaultPath ?? quest.Nodes.Select(x => x.Id).ToArray();
        var lookup = quest.Nodes.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var steps = routeIds.Where(lookup.ContainsKey).Select(id => lookup[id]).ToArray();
        _loadingWalkthrough = true;
        FullQuestHeading.Text = $"{map.DisplayName} · {quest.DisplayName}";
        FullQuestSubheading.Text = routeNames.Length > 0
            ? $"Complete walkthrough · {_fullQuestRoute.Replace('_', ' ')} route · checkmarks are saved on this PC."
            : "Complete walkthrough · checkmarks are saved on this PC.";
        FullQuestRouteTabs.ItemsSource = routeNames.Select(route => new WalkthroughRouteChoice(route, FriendlyRoute(route))).ToArray();
        FullQuestSteps.ItemsSource = steps.Select((step, index) => new WalkthroughStepViewModel(
            step, index, _checklistState.GetValueOrDefault(CheckKey(_activeGame, map.DisplayName, quest.DisplayName, _fullQuestRoute, step.Id)),
            _activeGame, map.DisplayName, quest.DisplayName, _fullQuestRoute,
            item => _checklistState.GetValueOrDefault($"{_activeGame}|{map.DisplayName}|{quest.DisplayName}|{step.Id}|item|{item.Id}"))).ToArray();
        _loadingWalkthrough = false;
    }

    private static string FriendlyRoute(string route) => route switch
    {
        "maxis" => "Maxis", "richtofen" => "Richtofen", "stand_in" => "Stand-In", "ensemble_cast" => "Ensemble Cast",
        "continue_cycle" => "Continue the Cycle", "break_cycle" => "Break the Cycle", "vanilla" => "Vanilla", "any_player_ee" => "Any Player EE", "any_player_ee_sr" => "Any Player EE SR",
        _ => route.Replace('_', ' ')
    };

    private void FullQuestStepToggled(object sender, RoutedEventArgs e)
    {
        if (_loadingWalkthrough || sender is not System.Windows.Controls.CheckBox { DataContext: WalkthroughStepViewModel vm } checkBox || FullQuestMapBox.SelectedItem is not WalkthroughMapChoice choice) return;
        var map = choice.Index < 0 ? null : QuestFlowCatalog.LoadAll().ElementAtOrDefault(choice.Index);
        var quest = map?.Quests.FirstOrDefault();
        if (map is null || quest is null) return;
        vm.IsChecked = checkBox.IsChecked == true;
        var key = CheckKey(_activeGame, map.DisplayName, quest.DisplayName, _fullQuestRoute, vm.Step.Id);
        if (vm.IsChecked) _checklistState[key] = true; else _checklistState.Remove(key);
        Directory.CreateDirectory(Path.GetDirectoryName(_checklistPath)!);
        File.WriteAllText(_checklistPath, JsonSerializer.Serialize(_checklistState, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void FullQuestItemToggled(object sender, RoutedEventArgs e)
    {
        if (_loadingWalkthrough || sender is not System.Windows.Controls.CheckBox { DataContext: WalkthroughChecklistItemViewModel vm }) return;
        vm.IsChecked = ((System.Windows.Controls.CheckBox)sender).IsChecked == true;
        if (vm.IsChecked) _checklistState[vm.Key] = true; else _checklistState.Remove(vm.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(_checklistPath)!);
        File.WriteAllText(_checklistPath, JsonSerializer.Serialize(_checklistState, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string CheckKey(string game, string map, string quest, string route, string step) => $"{game}|{map}|{quest}|{route}|{step}";

    private static string PrepKey(string game, string map, string quest, string scope, string? route, string id) =>
        $"prep|{game}|{map}|{quest}|{scope}|{(scope == "shared" ? "shared" : route ?? "unselected")}|{id}";

    private void PreparationRequirementToggled(object sender, RoutedEventArgs e)
    {
        if (_loadingPreparation || sender is not System.Windows.Controls.CheckBox { DataContext: PreparationItemViewModel vm } checkBox) return;
        vm.IsChecked = checkBox.IsChecked == true;
        if (vm.IsChecked) _checklistState[vm.Key] = true; else _checklistState.Remove(vm.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(_checklistPath)!);
        File.WriteAllText(_checklistPath, JsonSerializer.Serialize(_checklistState, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void SideEggMapChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SideEggMapBox.IsLoaded) RefreshSideEggs();
    }

    private void ApplySideEggEvent(TelemetryEvent e)
    {
        if (e.Type != "side_egg_step" || string.IsNullOrWhiteSpace(e.Map) || string.IsNullOrWhiteSpace(e.EggId) || e.StepIndex is not >= 0) return;
        var game = e.Game == "bo2" || e.Map.StartsWith("zm_", StringComparison.Ordinal) ? "bo2" : "bo1";
        var mapId = e.Map switch
        {
            "zombie_cosmodrome" => "bo1_ascension",
            "zombie_coast" => "bo1_call_of_the_dead",
            "zombie_temple" => "bo1_shangri_la",
            "zombie_moon" => "bo1_moon",
            "zm_transit" => "bo2_transit",
            "zm_prison" => "bo2_mob",
            "zm_tomb" => "bo2_origins",
            _ => e.Map
        };
        var map = SideEggMaps(game).FirstOrDefault(m => m.MapId == mapId || m.Label == e.Map);
        if (map is null) return;
        var key = $"side|{game}|{map.MapId}|{e.EggId}|{e.StepIndex.Value}";
        _checklistState[key] = true;
        Directory.CreateDirectory(Path.GetDirectoryName(_checklistPath)!);
        File.WriteAllText(_checklistPath, JsonSerializer.Serialize(_checklistState, new JsonSerializerOptions { WriteIndented = true }));
        if (SideEggMapBox.SelectedItem is WalkthroughMapChoice selected && selected.MapId == map.MapId) RefreshSideEggs();
    }

    private void RefreshSideEggs()
    {
        if (SideEggMapBox?.SelectedItem is not WalkthroughMapChoice choice) return;
        var path = Path.Combine(AppContext.BaseDirectory, "data", "side-easter-egg-catalog.json");
        if (!File.Exists(path)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var map = doc.RootElement.GetProperty("games").EnumerateArray()
            .Where(g => g.GetProperty("gameId").GetString() == choice.GameId)
            .SelectMany(g => g.GetProperty("maps").EnumerateArray())
            .FirstOrDefault(m => m.GetProperty("mapId").GetString() == choice.MapId);
        if (map.ValueKind != JsonValueKind.Object) { SideEggSections.ItemsSource = Array.Empty<SideEggViewModel>(); return; }
        var eggs = map.GetProperty("eggs").EnumerateArray().Select(egg =>
        {
            var eggId = egg.GetProperty("id").GetString()!;
            var steps = egg.GetProperty("steps").EnumerateArray().Select((step, index) =>
            {
                var key = $"side|{choice.GameId}|{choice.MapId}|{eggId}|{index}";
                var observerSignal = step.TryGetProperty("observerSignal", out var signalElement) ? signalElement.GetString() : null;
                var observerMap = choice.MapId switch
                {
                    "bo1_ascension" => "Ascension",
                    "bo1_call_of_the_dead" => "Call of the Dead",
                    "bo1_shangri_la" => "Shangri-La",
                    "bo1_moon" => "Moon",
                    "bo2_transit" => "zm_transit",
                    "bo2_mob" => "zm_prison",
                    "bo2_origins" => "zm_tomb",
                    _ => choice.MapId
                };
                var observed = observerSignal is not null && _engine.State.SideEggProgress.Any(p => p.Map == observerMap && p.EggId == eggId && p.StepIndex == index);
                return new SideEggStepViewModel(key, step.GetProperty("label").GetString()!, step.GetProperty("location").GetString()!, _checklistState.GetValueOrDefault(key), observed, observerSignal is not null);
            }).ToArray();
            return new SideEggViewModel(egg.GetProperty("title").GetString()!, egg.GetProperty("scriptEvidence").GetString()!, steps);
        }).ToArray();
        SideEggSections.ItemsSource = eggs;
    }

    private void SideEggStepToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.CheckBox { DataContext: SideEggStepViewModel step } checkBox) return;
        if (step.Observed) { checkBox.IsChecked = true; return; }
        step.IsChecked = checkBox.IsChecked == true;
        if (step.IsChecked) _checklistState[step.Key] = true; else _checklistState.Remove(step.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(_checklistPath)!);
        File.WriteAllText(_checklistPath, JsonSerializer.Serialize(_checklistState, new JsonSerializerOptions { WriteIndented = true }));
    }
    private async void LoadReplay(object sender, RoutedEventArgs e) => await StartSource(new JsonlReplaySource(PathBox.Text, TimeSpan.FromMilliseconds(450)), "Saved session");
    private async void LoadBuiltInSample(object sender, RoutedEventArgs e)
    {
        if (SampleBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string fileName) return;
        PathBox.Text = Path.Combine(AppContext.BaseDirectory, "samples", fileName);
        await StartSource(new JsonlReplaySource(PathBox.Text, TimeSpan.FromMilliseconds(450)), $"{item.Content} replay");
    }
    private async void LoadTail(object sender, RoutedEventArgs e) => await StartSource(new JsonlTailSource(PathBox.Text), "Session file");
    private async void SelectBo1(object sender, RoutedEventArgs e) => await SwitchGameAsync("bo1");
    private async void SelectBo2(object sender, RoutedEventArgs e) => await SwitchGameAsync("bo2");
    private async void ConnectSelectedGame(object sender, RoutedEventArgs e) => await ConnectSelectedGameAsync();

    private async Task SwitchGameAsync(string game)
    {
        if (_activeGame == game) return;
        _activeGame = game;
        var maps = _walkthroughMaps;
        var selectedName = (FullQuestMapBox.SelectedItem as WalkthroughMapChoice)?.Label;
        var filtered = maps.Where(x => x.GameId == game).ToArray();
        if (filtered.Length > 0)
        {
            var pick = filtered.FirstOrDefault(x => x.Label == selectedName) ?? filtered[0];
            FullQuestMapBox.ItemsSource = filtered;
            SideEggMapBox.ItemsSource = SideEggMaps(game);
            FullQuestMapBox.SelectedItem = pick;
            SideEggMapBox.SelectedItem = SideEggMaps(game).FirstOrDefault(x => x.Label == selectedName) ?? SideEggMaps(game).FirstOrDefault();
            _fullQuestRoute = "";
            RefreshFullQuest();
            RefreshSideEggs();
        }
        if (game != "bo2") StopT6Collector();
        Bo1Tab.Style = (Style)FindResource(game == "bo1" ? "RailActiveButton" : "RailButton");
        Bo2Tab.Style = (Style)FindResource(game == "bo2" ? "RailActiveButton" : "RailButton");
        SetPage(MissionPage);
        await ConnectSelectedGameAsync();
    }

    private async Task ConnectSelectedGameAsync()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var gameFolder = _activeGame == "bo1" ? "t5" : "t6";
        var plutoniumStorage = Path.Combine(localAppData, "Plutonium", "storage", gameFolder);
        var fileName = _activeGame == "bo1" ? "ee-tracker.jsonl" : "ee-tracker-bo2.jsonl";
        var filePath = Path.Combine(plutoniumStorage, "raw", "scriptdata", fileName);
        PathBox.Text = filePath;
        var label = _activeGame == "bo1" ? "BO1 game session" : "BO2 game session";
        if (_activeGame == "bo2")
        {
            StartT6Collector();
            SettingsStatus.Text = "Select BO2 before launching a match on an observer-supported map. T6 collector started; waiting for records.";
        }
        await StartSource(new JsonlTailSource(filePath, readExisting: true, idleTimeout: TimeSpan.FromSeconds(7)), label);
        if (_activeGame == "bo2" && _t6Collector is { HasExited: true })
            SettingsStatus.Text = "T6 collector exited. Confirm Plutonium console.log exists, then reconnect.";
    }

    private void StartT6Collector()
    {
        if (_t6Collector is { HasExited: false }) return;
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "gsc", "t6", "Collect-EETrackerT6Telemetry.ps1");
        if (!File.Exists(scriptPath))
        {
            SettingsStatus.Text = $"T6 collector was not found: {scriptPath}";
            return;
        }

        var powerShellPath = Path.Combine(AppContext.BaseDirectory, "pwsh.exe");
        if (!File.Exists(powerShellPath)) powerShellPath = Environment.GetEnvironmentVariable("EE_TRACKER_POWERSHELL") ?? "powershell.exe";
        var startInfo = new ProcessStartInfo(powerShellPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        try
        {
            _t6Collector = Process.Start(startInfo);
            if (_t6Collector is null) SettingsStatus.Text = "Could not start the T6 telemetry collector.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            SettingsStatus.Text = $"Could not start the T6 telemetry collector: {ex.Message}";
        }
    }

    private void StopT6Collector()
    {
        if (_t6Collector is null) return;
        try
        {
            if (!_t6Collector.HasExited) _t6Collector.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        _t6Collector.Dispose();
        _t6Collector = null;
    }
    private static string FriendlyActivity(TelemetryEvent e) => e.Signal switch
    {
        "ascension.monkey_button_interaction" => "Monkey switch pressed",
        _ when e.Type == "session_started" => "Session started",
        _ when e.Type == "session_ended" => "Session ended",
        _ when e.Type == "transport_status" => e.SignalValue == "connected" ? "Game reconnected" : "Game stopped responding",
        _ when e.Type == "pressure_timer" => "Pressure pad timer updated",
        _ when e.Type == "luna_progress" => e.LettersCollected == 0 ? "LUNA sequence ready or reset" : $"LUNA letters collected: {e.LettersCollected}/4",
        _ when e.Type == "samantha_color" => $"Samantha Says color: {e.SignalValue}",
        _ when e.Type == "soul_tank_progress" => "Moon soul tube fill updated",
        _ when e.Type == "quest_progress" => "Quest substep progress updated",
        _ when e.Type == "richtofen_cue" => "Richtofen's quest dialogue cue observed",
        _ => "Game status updated"
    };
    private void ApplyPathChoice(object sender, RoutedEventArgs e)
    {
        if (PathChoiceBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string path) return;
        _engine.Apply(new TelemetryEvent { TimestampUtc = DateTimeOffset.UtcNow, SessionId = "manual-path-choice", Type = "quest_path_selected", SignalValue = path, Source = "manual selection" });
        Refresh();
    }
    private void BrowseSource(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON Lines (*.jsonl)|*.jsonl|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) PathBox.Text = dialog.FileName;
    }

    private void SaveWindowCapture(string path)
    {
        UpdateLayout();
        var scale = VisualTreeHelper.GetDpi(this);
        var bitmap = new RenderTargetBitmap((int)(ActualWidth * scale.DpiScaleX), (int)(ActualHeight * scale.DpiScaleY), scale.PixelsPerInchX, scale.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(this);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}

public sealed record WalkthroughMapChoice(string Label, int Index, string GameId, string MapId)
{
    public override string ToString() => Label;
}

public sealed record WalkthroughRouteChoice(string Key, string Label);

public sealed class SideEggViewModel
{
    public string Title { get; }
    public string Evidence { get; }
    public IReadOnlyList<SideEggStepViewModel> Steps { get; }
    public SideEggViewModel(string title, string evidence, IReadOnlyList<SideEggStepViewModel> steps)
    { Title = title; Evidence = evidence; Steps = steps; }
}

public sealed class SideEggStepViewModel
{
    public string Key { get; }
    public string Label { get; }
    public string Location { get; }
    public bool IsChecked { get; set; }
    public bool Observed { get; }
    public string TrackingStatus { get; }
    public SideEggStepViewModel(string key, string label, string location, bool isChecked, bool observed, bool observerSupported)
    { Key = key; Label = label; Location = location; IsChecked = isChecked || observed; Observed = observed; TrackingStatus = observed ? "OBSERVED" : observerSupported ? "AUTO" : "MANUAL"; }
}

public sealed class WalkthroughStepViewModel
{
    public FlowNode Step { get; }
    public string Title { get; }
    public string Instruction { get; }
    public string Detection { get; }
    public bool IsChecked { get; set; }
    public IReadOnlyList<WalkthroughChecklistViewModel> Checklists { get; }

    public WalkthroughStepViewModel(FlowNode step, int number, bool isChecked, string game, string map, string quest, string route, Func<FlowChecklistItem, bool> isItemChecked)
    {
        Step = step;
        Title = $"{number + 1:00}  ·  {step.Title}";
        Instruction = step.Instruction;
        Detection = string.IsNullOrWhiteSpace(step.Detection) ? "" : $"TRACKER SIGNAL · {step.Detection}";
        IsChecked = isChecked;
        Checklists = step.Checklists.Select(group => new WalkthroughChecklistViewModel(group, step.Id, game, map, quest, route, isItemChecked)).ToArray();
    }
}

public sealed class WalkthroughChecklistViewModel
{
    public string Title { get; }
    public IReadOnlyList<WalkthroughChecklistItemViewModel> Items { get; }
    public WalkthroughChecklistViewModel(FlowChecklist source, string stepId, string game, string map, string quest, string route, Func<FlowChecklistItem, bool> isChecked)
    {
        Title = source.Title;
        Items = source.Items.Select(item => new WalkthroughChecklistItemViewModel(
            $"{game}|{map}|{quest}|{stepId}|item|{item.Id}", item.Label, item.Location, isChecked(item))).ToArray();
    }
}

public sealed class WalkthroughChecklistItemViewModel : INotifyPropertyChanged
{
    public string Key { get; }
    public string Label { get; }
    public string Location { get; }
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked == value) return; _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextDecorations))); }
    }
    public TextDecorationCollection TextDecorations => IsChecked ? System.Windows.TextDecorations.Strikethrough : new TextDecorationCollection();
    public event PropertyChangedEventHandler? PropertyChanged;
    public WalkthroughChecklistItemViewModel(string key, string label, string location, bool isChecked)
    { Key = key; Label = label; Location = location; IsChecked = isChecked; }
}

public sealed class StepTrackerViewModel
{
    public string Title { get; }
    public string Summary { get; }
    public int Progress { get; }
    public int Maximum { get; }
    public Visibility ProgressBarVisibility { get; }
    public IReadOnlyList<StepTrackerCheckpointViewModel> Checkpoints { get; }

    public StepTrackerViewModel(StepTrackerState state)
    {
        Title = state.Title;
        Summary = state.Summary;
        Progress = state.Progress;
        Maximum = state.Maximum;
        ProgressBarVisibility = state.Maximum > 1 ? Visibility.Visible : Visibility.Collapsed;
        Checkpoints = state.Checkpoints.Select(x => new StepTrackerCheckpointViewModel(x)).ToArray();
    }
}

public sealed record ObjectiveChecklistItemViewModel(string Label, string Location, bool IsChecked)
{
    public string Marker => IsChecked ? "✓" : "○";
    public Brush MarkerBrush => IsChecked ? Brushes.DarkSeaGreen : Brushes.Gray;
}

public sealed record ObjectiveChecklistViewModel(string Title, IReadOnlyList<ObjectiveChecklistItemViewModel> Items);

public sealed class StepTrackerCheckpointViewModel
{
    public string Label { get; }
    public string Marker { get; }
    public string Status { get; }
    public Brush MarkerBrush { get; }

    public StepTrackerCheckpointViewModel(StepCheckpointState state)
    {
        Label = state.Label;
        Marker = state.Complete ? "✓" : "○";
        Status = state.Complete ? "COMPLETE" : "WAITING";
        MarkerBrush = state.Complete ? new SolidColorBrush(Color.FromRgb(128, 219, 160)) : new SolidColorBrush(Color.FromRgb(164, 176, 172));
    }
}

public sealed class PreparationItemViewModel : INotifyPropertyChanged
{
    public string Key { get; }
    public string Text { get; }
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked == value) return; _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public PreparationItemViewModel(string key, string text, bool isChecked) { Key = key; Text = text; IsChecked = isChecked; }
}

public sealed class ProgressStepViewModel
{
    public string ShortTitle { get; }
    public string Status { get; }
    public string Marker { get; }
    public Brush MarkerBrush { get; }
    public Brush TitleBrush { get; }
    public double Width { get; }
    public Brush MarkerFill { get; }
    public Brush ConnectorBrush { get; }

    public ProgressStepViewModel(QuestStep step, int number, int columns)
    {
        Width = columns == 2 ? 310 : 360;
        ShortTitle = step.Title.Replace("Node ", "", StringComparison.Ordinal).Replace(" — ", " · ");
        Status = step.Status.ToUpperInvariant();
        Marker = step.Status switch { "Complete" => "✓", "Current" => $"{number:00}", _ => "·" };
        MarkerBrush = step.Status switch { "Complete" => Brushes.LightGreen, "Current" => Brushes.Gold, _ => Brushes.DimGray };
        TitleBrush = step.Status == "Current" ? Brushes.White : Brushes.LightGray;
        MarkerFill = step.Status switch { "Complete" => new SolidColorBrush(Color.FromRgb(41, 74, 48)), "Current" => new SolidColorBrush(Color.FromRgb(70, 57, 28)), _ => new SolidColorBrush(Color.FromRgb(31, 38, 34)) };
        ConnectorBrush = step.Status == "Upcoming" ? new SolidColorBrush(Color.FromRgb(55, 64, 58)) : new SolidColorBrush(Color.FromRgb(105, 150, 106));
    }
}




