using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using EETracker.Core;
using Microsoft.Win32;

namespace EETracker.App;

public partial class MainWindow : Window
{
    private QuestEngine _engine = new();
    private CancellationTokenSource? _run;
    private string _lastEvent = "No activity yet";
    private string _sourceName = "No source";

    public MainWindow()
    {
        InitializeComponent();
        PathBox.Text = Path.Combine(AppContext.BaseDirectory, "samples", "ascension-session.jsonl");
        Refresh();
        SizeChanged += (_, _) => UpdateTrackerLayout();
        Loaded += async (_, _) =>
        {
            var args = Environment.GetCommandLineArgs();
            var previewIndex = Array.IndexOf(args, "--preview");
            var captureIndex = Array.IndexOf(args, "--capture-layout");
            if (previewIndex >= 0 && previewIndex + 1 < args.Length)
            {
                PathBox.Text = Path.GetFullPath(args[previewIndex + 1]);
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
            await ConnectT5Session();
        };
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
        source.EventReceived += e => Dispatcher.Invoke(() => { if (!ReferenceEquals(_run, run)) return; _engine.Apply(e); _lastEvent = $"{e.TimestampUtc:HH:mm:ss}  {FriendlyActivity(e)}"; SettingsStatus.Text = $"Receiving {label.ToLowerInvariant()} data"; Refresh(); });
        SettingsStatus.Text = label == "Game session" ? $"Waiting for game data at {PathBox.Text}" : $"Source: {label}";
        Refresh();
        try { await source.StartAsync(run.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Dispatcher.Invoke(() => { if (!ReferenceEquals(_run, run)) return; SettingsStatus.Text = $"Could not open this session: {ex.Message}"; _lastEvent = "Session could not be opened"; Refresh(); }); }
        finally { await source.DisposeAsync(); }
    }

    private void Refresh()
    {
        var s = _engine.State;
        MapTitle.Text = s.Map.ToUpperInvariant();
        QuestTitle.Text = s.QuestName.ToUpperInvariant();
        ApplyMapBanner(s.Map);
        RoundText.Text = s.Round?.ToString() ?? "—";
        PlayersText.Text = s.PlayerCount?.ToString() ?? "—";
        RouteText.Text = s.RequiresPathChoice ? "CHOOSE ROUTE" : s.Map == "Call of the Dead" ? s.PlayerCount switch { 1 => "STAND-IN", > 1 => "ENSEMBLE CAST", _ => "DETECTING" } : "AUTOMATIC";
        PowerText.Text = s.PowerOn switch { true => "ON", false => "OFF", _ => "—" };
        ConnectionText.Text = s.Connected ? "CONNECTED" : s.SessionEnded ? "GAME ENDED" : s.ConnectionInterrupted ? "NO GAME SIGNAL" : "WAITING";
        ConnectionDot.Fill = s.Connected ? (Brush)FindResource("AccentOlive") : (Brush)FindResource("AccentAmber");
        ObjectiveText.Text = s.CurrentObjective;
        InstructionText.Text = s.Instruction;
        var objectiveTracker = s.CurrentTrackers.FirstOrDefault();
        TrackerProgressPanel.Visibility = objectiveTracker is null ? Visibility.Collapsed : Visibility.Visible;
        if (objectiveTracker is not null)
        {
            var hasCount = objectiveTracker.Maximum > 1;
            QuestRingContainer.Visibility = hasCount ? Visibility.Visible : Visibility.Collapsed;
            QuestRingText.Text = hasCount ? $"{objectiveTracker.Progress} / {objectiveTracker.Maximum}" : objectiveTracker.Summary;
            QuestRingLabel.Text = hasCount ? objectiveTracker.Title : "OBJECTIVE CHECKS";
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
        UpdateTrackerLayout();
        PreparationItems.ItemsSource = s.Preparation.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => new PreparationItemViewModel(line, s.PowerOn)).ToArray();
        NextText.Text = s.NextStep.StartsWith("After this: ", StringComparison.Ordinal)
            ? s.NextStep["After this: ".Length..] : s.NextStep;
        var currentStep = Array.FindIndex(s.Progression.ToArray(), step => step.Status == "Current");
        var nextStep = currentStep >= 0 && currentStep + 1 < s.Progression.Count ? s.Progression[currentStep + 1] : null;
        NextInstructionText.Visibility = nextStep is null ? Visibility.Collapsed : Visibility.Visible;
        NextInstructionText.Text = nextStep?.Instruction ?? "";
        PreviousText.Text = $"Previous: {s.PreviousStep}";
        PathChoicePanel.Visibility = s.RequiresPathChoice ? Visibility.Visible : Visibility.Collapsed;
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

    private void ApplyMapBanner(string map)
    {
        var asset = map switch
        {
            "Call of the Dead" => "call-of-the-dead-banner.png",
            "Shangri-La" => "shangri-la-banner.png",
            "Moon" => "moon-banner.png",
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
    private void ShowSettings(object sender, RoutedEventArgs e) => SetPage(SettingsPage);
    private void ShowDiagnostics(object sender, RoutedEventArgs e) => SetPage(DiagnosticsPage);
    private void SetPage(UIElement page) { MissionPage.Visibility = Visibility.Collapsed; SettingsPage.Visibility = Visibility.Collapsed; DiagnosticsPage.Visibility = Visibility.Collapsed; page.Visibility = Visibility.Visible; }
    private async void LoadReplay(object sender, RoutedEventArgs e) => await StartSource(new JsonlReplaySource(PathBox.Text, TimeSpan.FromMilliseconds(450)), "Saved session");
    private async void LoadBuiltInSample(object sender, RoutedEventArgs e)
    {
        if (SampleBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string fileName) return;
        PathBox.Text = Path.Combine(AppContext.BaseDirectory, "samples", fileName);
        await StartSource(new JsonlReplaySource(PathBox.Text, TimeSpan.FromMilliseconds(450)), $"{item.Content} replay");
    }
    private async void LoadTail(object sender, RoutedEventArgs e) => await StartSource(new JsonlTailSource(PathBox.Text), "Session file");
    private async void ConnectT5File(object sender, RoutedEventArgs e) => await ConnectT5Session();
    private async Task ConnectT5Session()
    {
        var t5Storage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Plutonium", "storage", "t5");
        PathBox.Text = Path.Combine(t5Storage, "raw", "scriptdata", "ee-tracker.jsonl");
        await StartSource(new JsonlTailSource(PathBox.Text, readExisting: true, idleTimeout: TimeSpan.FromSeconds(7)), "Game session");
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

public sealed class PreparationItemViewModel
{
    public string Text { get; }
    public string Marker { get; }
    public string Status { get; }
    public Brush MarkerBrush { get; }

    public PreparationItemViewModel(string requirement, bool? powerOn)
    {
        Text = requirement.Trim().TrimStart('•').Trim();
        var powerRequirement = Text.Equals("Turn on the power.", StringComparison.OrdinalIgnoreCase)
            || Text.Equals("Turn on the power before continuing.", StringComparison.OrdinalIgnoreCase)
            || Text.Equals("Power on.", StringComparison.OrdinalIgnoreCase);
        var information = Text.StartsWith("Stand-In is the solo quest", StringComparison.OrdinalIgnoreCase);
        var unknown = Text.Contains("not yet observed", StringComparison.OrdinalIgnoreCase)
            || Text.Contains("depend on", StringComparison.OrdinalIgnoreCase)
            || Text.Contains("unavailable", StringComparison.OrdinalIgnoreCase);
        var complete = powerRequirement && powerOn == true;
        Marker = complete ? "✓" : information ? "·" : "○";
        Status = complete ? "COMPLETE" : information ? "INFO" : unknown ? "UNKNOWN" : "TO CHECK";
        MarkerBrush = complete ? new SolidColorBrush(Color.FromRgb(128, 219, 160))
            : unknown ? new SolidColorBrush(Color.FromRgb(164, 176, 172))
            : new SolidColorBrush(Color.FromRgb(243, 211, 100));
    }
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




