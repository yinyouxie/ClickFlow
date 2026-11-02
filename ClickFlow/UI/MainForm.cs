using System.Runtime.InteropServices;
using System.Text;
using ClickFlow.Core;
using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed partial class MainForm : Form
{
    private const int HotkeyRecord = 0x5A01;
    private const int HotkeyPlay = 0x5A02;
    private const int HotkeyStop = 0x5A03;
    private const int HotkeyAuto = 0x5A04;
    private const int HotkeyCapture = 0x5A05;
    private const int HotkeyRecordAlt = 0x5A11;
    private const int HotkeyPlayAlt = 0x5A12;
    private const int HotkeyStopAlt = 0x5A13;
    private const int HotkeyAutoAlt = 0x5A14;
    private const int HotkeyCaptureAlt = 0x5A15;
    private const int HotkeyStopF6 = 0x5A16;

    private static readonly int[] DelayValues = { 0, 3, 5 };
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(4);

    private readonly MacroRecorder _recorder = new();
    private readonly MouseKeyboardPlayer _player = new();
    private readonly AutoClicker _autoClicker = new();
    private readonly InputGuard _inputGuard = new();
    private readonly AppSettings _settings;
    private readonly List<RecordingCard> _historyCards = new();
    private readonly System.Windows.Forms.Timer _uiTimer;
    private readonly ToolTip _toolTip = new();
    private readonly string? _initialFile;

    private MacroDocument _document = new();
    private RecordingEntry? _selectedEntry;
    private CancellationTokenSource? _cts;
    private Task? _operationTask;
    private RunState _state = RunState.Idle;
    private AppMode _mode = AppMode.Macro;
    private string _targetTitle = string.Empty;
    private StatusHud? _hud;
    private DateTime _toastUntil = DateTime.MinValue;
    private string? _toastText;
    private string _lastPillText = string.Empty;
    private bool? _lastUiIdle;
    private string _lastHudSignature = string.Empty;
    private string _lastFooterText = string.Empty;
    private float _pulse;
    private long _playbackPercent;
    private int _playbackLoop;
    private int _playbackLoops = 1;
    private bool _suppressUi;
    private bool _minimizedForRun;
    private bool _loaded;
    private bool _closing;
    private Image? _backdrop;

    private bool _hotkeyRecord;
    private bool _hotkeyPlay;
    private bool _hotkeyStop;
    private bool _hotkeyAuto;
    private bool _hotkeyCapture;
    private bool _hotkeyRecordAlt;
    private bool _hotkeyPlayAlt;
    private bool _hotkeyStopAlt;
    private bool _hotkeyAutoAlt;
    private bool _hotkeyCaptureAlt;
    private bool _hotkeyStopF6;

    public MainForm(string? initialFile = null)
    {
        _initialFile = initialFile;
        _settings = MacroStore.LoadSettings();
        L.SetLanguage(_settings.Language);
        Theme.ApplyFontScale((float)_settings.UiScale);
        Theme.Apply(Theme.Resolve(_settings.ThemeId, _settings.CustomThemeArgb));
        BuildUi();
        L.Apply(this);
        UpdateBackdrop();
        WireEvents();
        ApplySettingsToUi();

        _uiTimer = new System.Windows.Forms.Timer { Interval = 80 };
        _uiTimer.Tick += UiTimer_Tick;
        _recorder.EventLimitReached += OnEventLimitReached;

        try
        {
            using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("ClickFlow.app.ico");
            if (stream != null)
            {
                Icon = new Icon(stream);
            }
        }
        catch
        {
            // The embedded executable icon still applies if this fails.
        }

        UpdateUiEnabled();
        RefreshShell();
    }

    private string RecordKeyLabel => _hotkeyRecord ? "F1" : _hotkeyRecordAlt ? "Ctrl+F1" : "—";
    private string PlayKeyLabel => _hotkeyPlay ? "F2" : _hotkeyPlayAlt ? "Ctrl+F2" : "—";
    private string StopKeyLabel => _hotkeyStop ? "F3" : _hotkeyStopAlt ? "Ctrl+F3" : _hotkeyStopF6 ? "F6" : "—";
    private string AutoKeyLabel => _hotkeyAuto ? "F4" : _hotkeyAutoAlt ? "Ctrl+F4" : "—";
    private string CaptureKeyLabel => _hotkeyCapture ? "F5" : _hotkeyCaptureAlt ? "Ctrl+F5" : "—";
    private bool StopHotkeyAvailable => _hotkeyStop || _hotkeyStopAlt || _hotkeyStopF6;
    private bool RecordHotkeyAvailable => _hotkeyRecord || _hotkeyRecordAlt;
    private bool PlayHotkeyAvailable => _hotkeyPlay || _hotkeyPlayAlt;
    private bool AutoHotkeyAvailable => _hotkeyAuto || _hotkeyAutoAlt;

    private enum RunState
    {
        Idle,
        Preparing,
        Recording,
        Playing,
        AutoClicking
    }

    private enum AppMode
    {
        Macro = 0,
        Auto = 1
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowEffects.ApplyDarkChrome(this);
        RegisterHotkeys();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateBackdrop();
    }

    private void UpdateBackdrop()
    {
        var next = Theme.CreateBackdrop(ClientSize);
        var previous = _backdrop;
        _backdrop = next;
        BackgroundImage = next;
        previous?.Dispose();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnregisterHotkeys();
        base.OnHandleDestroyed(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _uiTimer.Start();
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        ClampToWorkingArea();
        RefreshHistory();

        if (!string.IsNullOrWhiteSpace(_initialFile))
        {
            ImportMacroFile(_initialFile, select: true);
        }
        else if (string.Equals(_settings.StartupPage, "auto", StringComparison.OrdinalIgnoreCase))
        {
            _nav.SelectedIndex = 1;
        }

        if (_settings.CheckUpdatesOnStartup)
        {
            _ = CheckUpdatesOnStartupAsync();
        }



        var missing = new List<string>();
        if (!RecordHotkeyAvailable) missing.Add("F1/Ctrl+F1");
        if (!PlayHotkeyAvailable) missing.Add("F2/Ctrl+F2");
        if (!StopHotkeyAvailable) missing.Add("F3/Ctrl+F3/F6");
        if (!AutoHotkeyAvailable) missing.Add("F4/Ctrl+F4");
        if (!_hotkeyCapture && !_hotkeyCaptureAlt) missing.Add("F5/Ctrl+F5");
        if (missing.Count > 0)
        {
            ShowToast(L.T(
                "快捷键被占用：" + string.Join("、", missing) + "，仍可使用窗口按钮",
                "Hotkeys occupied: " + string.Join(", ", missing) + "; window buttons still work"), 9);
        }
        else if (!_hotkeyRecord || !_hotkeyPlay || !_hotkeyStop ||
                 !_hotkeyAuto || !_hotkeyCapture)
        {
            ShowToast(L.T(
                "F1–F5 被占用 · 已启用 Ctrl+F1–F5",
                "F1–F5 occupied · using Ctrl+F1–F5"), 8);
        }

        UpdateHotkeyLabels();
        RefreshShell();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _closing = true;
        _uiTimer.Stop();
        _cts?.Cancel();

        if (_recorder.IsRecording)
        {
            try
            {
                var events = _recorder.Stop();
                if (events.Count > 0)
                {
                    RecordingLibrary.Add(BuildDocument(events));
                }
            }
            catch
            {
                // Closing must not be blocked by an archive failure.
            }
        }

        _inputGuard.Stop();

        try
        {
            _operationTask?.Wait(1200);
        }
        catch
        {
            // Best effort while shutting down.
        }

        CollectSettings();
        MacroStore.SaveSettings(_settings);
        _hud?.HideHud();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _inputGuard.Dispose();
        _hud?.Dispose();
        _toolTip.Dispose();
        _recorder.Dispose();
        _uiTimer.Dispose();
        base.OnFormClosed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();
            BeginInvoke(new Action(() => HandleHotkey(id)));
            return;
        }

        base.WndProc(ref m);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.O))
        {
            ImportMacroDialog();
            return true;
        }

        if (keyData == (Keys.Control | Keys.H))
        {
            ShowHelp();
            return true;
        }

        if (keyData == Keys.Escape && _state != RunState.Idle)
        {
            StopEverything();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void RegisterHotkeys()
    {
        if (Handle == IntPtr.Zero)
        {
            return;
        }

        UnregisterHotkeys();

        _hotkeyRecord = NativeMethods.RegisterHotKey(
            Handle, HotkeyRecord, NativeMethods.MOD_NOREPEAT, (uint)Keys.F1);
        var recordError = _hotkeyRecord ? 0 : Marshal.GetLastWin32Error();
        _hotkeyRecordAlt = !_hotkeyRecord && NativeMethods.RegisterHotKey(
            Handle,
            HotkeyRecordAlt,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.F1);

        _hotkeyPlay = NativeMethods.RegisterHotKey(
            Handle, HotkeyPlay, NativeMethods.MOD_NOREPEAT, (uint)Keys.F2);
        var playError = _hotkeyPlay ? 0 : Marshal.GetLastWin32Error();
        _hotkeyPlayAlt = !_hotkeyPlay && NativeMethods.RegisterHotKey(
            Handle,
            HotkeyPlayAlt,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.F2);

        _hotkeyStop = NativeMethods.RegisterHotKey(
            Handle, HotkeyStop, NativeMethods.MOD_NOREPEAT, (uint)Keys.F3);
        var stopError = _hotkeyStop ? 0 : Marshal.GetLastWin32Error();
        _hotkeyStopAlt = !_hotkeyStop && NativeMethods.RegisterHotKey(
            Handle,
            HotkeyStopAlt,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.F3);
        _hotkeyStopF6 = !_hotkeyStop && !_hotkeyStopAlt && NativeMethods.RegisterHotKey(
            Handle, HotkeyStopF6, NativeMethods.MOD_NOREPEAT, (uint)Keys.F6);

        _hotkeyAuto = NativeMethods.RegisterHotKey(
            Handle, HotkeyAuto, NativeMethods.MOD_NOREPEAT, (uint)Keys.F4);
        var autoError = _hotkeyAuto ? 0 : Marshal.GetLastWin32Error();
        _hotkeyAutoAlt = !_hotkeyAuto && NativeMethods.RegisterHotKey(
            Handle,
            HotkeyAutoAlt,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.F4);

        _hotkeyCapture = NativeMethods.RegisterHotKey(
            Handle, HotkeyCapture, NativeMethods.MOD_NOREPEAT, (uint)Keys.F5);
        var captureError = _hotkeyCapture ? 0 : Marshal.GetLastWin32Error();
        _hotkeyCaptureAlt = !_hotkeyCapture && NativeMethods.RegisterHotKey(
            Handle,
            HotkeyCaptureAlt,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_NOREPEAT,
            (uint)Keys.F5);

        try
        {
            Directory.CreateDirectory(MacroStore.AppDataDirectory);
            File.WriteAllText(
                Path.Combine(MacroStore.AppDataDirectory, "hotkeys.log"),
                $"F1={_hotkeyRecord} ({recordError}) Ctrl+F1={_hotkeyRecordAlt}\r\n" +
                $"F2={_hotkeyPlay} Ctrl+F2={_hotkeyPlayAlt}\r\n" +
                $"F3={_hotkeyStop} ({stopError}) Ctrl+F3={_hotkeyStopAlt} F6={_hotkeyStopF6}\r\n" +
                $"F4={_hotkeyAuto} Ctrl+F4={_hotkeyAutoAlt}\r\n" +
                $"F5={_hotkeyCapture} Ctrl+F5={_hotkeyCaptureAlt}\r\n");
        }
        catch
        {
            // Diagnostics only.
        }

        UpdateHotkeyLabels();
    }

    private void UnregisterHotkeys()
    {
        if (Handle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(Handle, HotkeyRecord);
        NativeMethods.UnregisterHotKey(Handle, HotkeyPlay);
        NativeMethods.UnregisterHotKey(Handle, HotkeyStop);
        NativeMethods.UnregisterHotKey(Handle, HotkeyAuto);
        NativeMethods.UnregisterHotKey(Handle, HotkeyCapture);
        NativeMethods.UnregisterHotKey(Handle, HotkeyRecordAlt);
        NativeMethods.UnregisterHotKey(Handle, HotkeyPlayAlt);
        NativeMethods.UnregisterHotKey(Handle, HotkeyStopAlt);
        NativeMethods.UnregisterHotKey(Handle, HotkeyAutoAlt);
        NativeMethods.UnregisterHotKey(Handle, HotkeyCaptureAlt);
        NativeMethods.UnregisterHotKey(Handle, HotkeyStopF6);
    }

    private void HandleHotkey(int id)
    {
        switch (id)
        {
            case HotkeyRecord:
            case HotkeyRecordAlt:
                if (_state == RunState.Recording)
                {
                    StopRecording();
                }
                else if (_state == RunState.Idle)
                {
                    RunSafe(ToggleRecordingAsync);
                }
                else
                {
                    ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
                }

                break;
            case HotkeyPlay:
            case HotkeyPlayAlt:
                if (_state == RunState.Playing)
                {
                    StopEverything();
                }
                else if (_state == RunState.Idle)
                {
                    RunSafe(TogglePlaybackAsync);
                }
                else
                {
                    ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
                }

                break;
            case HotkeyStop:
            case HotkeyStopAlt:
            case HotkeyStopF6:
                StopEverything();
                break;
            case HotkeyAuto:
            case HotkeyAutoAlt:
                if (_state is RunState.AutoClicking or RunState.Preparing)
                {
                    StopEverything();
                }
                else if (_state == RunState.Idle)
                {
                    SwitchMode(AppMode.Auto);
                    RunSafe(ToggleAutoAsync);
                }
                else
                {
                    ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
                }

                break;
            case HotkeyCapture:
            case HotkeyCaptureAlt:
                if (_state == RunState.Idle)
                {
                    CaptureCursorPosition(switchToAuto: true);
                }
                else
                {
                    ShowToast(L.T("录制或回放时无法修改连点坐标", "Cannot change click position while recording or playing"), 2);
                }

                break;
        }
    }

    private async void RunSafe(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        if (_state != RunState.Idle)
        {
            _pulse = (_pulse + 0.06f) % 1f;
            _statusPill.PulsePhase = _pulse;
            _statusPill.Pulse = true;
            _recordCard.PulsePhase = _pulse;
            _autoPulse.PulsePhase = _pulse;

            if (_state == RunState.AutoClicking)
            {
                _autoPulse.Count = _autoClicker.Count;
            }
        }

        if (_toastUntil != DateTime.MinValue && DateTime.Now >= _toastUntil)
        {
            _toastUntil = DateTime.MinValue;
            _toastText = null;
            UpdateFooter();
        }

        RefreshShell();
    }

    private void SetState(RunState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        _recordCard.IsRecording = state == RunState.Recording;
        _autoPulse.Running = state == RunState.AutoClicking;

        if (state != RunState.AutoClicking)
        {
            _autoPulse.Count = _autoClicker.Count;
        }

        UpdateUiEnabled();
        RefreshShell();
    }

    private void RefreshShell()
    {
        var text = L.T("待机", "Idle");
        var dot = Theme.Green;
        var pulse = false;
        double? hudProgress = null;
        var hudText = string.Empty;

        switch (_state)
        {
            case RunState.Preparing:
                text = L.T("准备中", "Preparing");
                dot = Theme.Amber;
                pulse = true;
                hudText = L.T($"准备开始 · {StopKeyLabel} 取消", $"Ready · {StopKeyLabel} cancel");
                break;
            case RunState.Recording:
                text = L.T("录制中 ", "Recording ") + Theme.FormatClock(_recorder.ElapsedMs);
                dot = Theme.Accent;
                pulse = true;
                hudText = L.T(
                    $"REC 录制中  {Theme.FormatClock(_recorder.ElapsedMs)}  ·  {StopHintForRecording()}",
                    $"REC  {Theme.FormatClock(_recorder.ElapsedMs)}  ·  {StopHintForRecording()}");
                break;
            case RunState.Playing:
                text = L.T($"回放中 {_playbackPercent}%", $"Playing {_playbackPercent}%");
                dot = Theme.Green;
                pulse = true;
                hudProgress = _playbackPercent / 100.0;
                hudText = L.T(
                    $"回放 {_playbackLoop}/{_playbackLoops} · {_playbackPercent}% · 点击 / {StopKeyLabel} 停止",
                    $"Playing {_playbackLoop}/{_playbackLoops} · {_playbackPercent}% · click / {StopKeyLabel} to stop");
                break;
            case RunState.AutoClicking:
                text = L.T($"连点中 {_autoClicker.Count:N0}", $"Clicking {_autoClicker.Count:N0}");
                dot = Theme.Green;
                pulse = true;
                hudText = L.T(
                    $"连点中  {_autoClicker.Count:N0} 次  ·  {StopHintForAuto()}",
                    $"Clicking  {_autoClicker.Count:N0}  ·  {StopHintForAuto()}");
                break;
        }

        if (_lastPillText != text)
        {
            _lastPillText = text;
            _statusPill.Text = text;
        }

        _statusPill.DotColor = dot;
        _statusPill.Pulse = pulse;

        _autoPulse.Count = _state == RunState.AutoClicking ? _autoClicker.Count : _autoClicker.Count;
        _autoPulse.Running = _state == RunState.AutoClicking;
        _autoPulse.Status = _state == RunState.AutoClicking
            ? L.T("正在连点", "Clicking")
            : _autoClicker.Count > 0
                ? L.T("已停止", "Stopped")
                : L.T("准备就绪", "Ready");
        _autoPulse.Detail = _state == RunState.AutoClicking
            ? L.T(
                $"每 {_autoInterval.Value} ms 点击一次 · 按 {StopKeyLabel} 停止",
                $"Click every {_autoInterval.Value} ms · {StopKeyLabel} to stop")
            : L.T(
                $"按 {AutoKeyLabel} 开始连点，{StopKeyLabel} 随时停止",
                $"Press {AutoKeyLabel} to start; {StopKeyLabel} stops");
        _autoPulse.PositionText = _autoPositionSeg.SelectedIndex == 0
            ? L.T("当前位置 · 跟随鼠标", "Current position · follow cursor")
            : L.T(
                $"固定坐标 · ({_autoX.Value}, {_autoY.Value})",
                $"Fixed position · ({_autoX.Value}, {_autoY.Value})");
        _autoStatusLabel.Text = _state switch
        {
            RunState.AutoClicking => L.T("运行中", "Running"),
            RunState.Preparing => L.T("准备中", "Preparing"),
            _ => L.T("就绪", "Ready")
        };
        _autoStatusLabel.ForeColor = _state == RunState.AutoClicking ? Theme.Green : Theme.Muted;

        UpdateHud(hudText, dot, hudProgress);
        UpdateFooter();
    }

    private void UpdateHud(string text, Color dot, double? progress)
    {
        if (_state == RunState.Idle)
        {
            _hud?.HideHud();
            _lastHudSignature = string.Empty;
            return;
        }

        _hud ??= new StatusHud();
        _hud.ShowStopAction = _state == RunState.Playing;
        var signature = text + "|" + progress;
        if (_lastHudSignature != signature)
        {
            _lastHudSignature = signature;
            _hud.SetStatus(text, dot, progress);
        }

        _hud.SetPulse(_pulse);
    }

    private void UpdateFooter()
    {
        if (DateTime.Now < _toastUntil && !string.IsNullOrEmpty(_toastText))
        {
            SetFooterText(_toastText, Theme.TextSoft);
            return;
        }

        var text = _mode == AppMode.Macro
            ? L.T(
                $"{RecordKeyLabel} 录制 · {PlayKeyLabel} 回放 · {StopKeyLabel} 随时停止",
                $"{RecordKeyLabel} record · {PlayKeyLabel} play · {StopKeyLabel} stop")
            : L.T(
                $"{AutoKeyLabel} 开始连点 · {CaptureKeyLabel} 捕获坐标 · {StopKeyLabel} 随时停止",
                $"{AutoKeyLabel} auto click · {CaptureKeyLabel} pick position · {StopKeyLabel} stop");
        var color = Theme.MutedDim;

        if (_state == RunState.Recording)
        {
            text = L.T(
                $"正在录制 · 按 {RecordKeyLabel} 或 {StopKeyLabel} 停止",
                $"Recording · press {RecordKeyLabel} or {StopKeyLabel} to stop");
            color = Theme.Muted;
        }
        else if (_state == RunState.Playing)
        {
            text = L.T(
                $"正在按原始时间轴回放 · 输入已锁定 · 按 {StopKeyLabel} 或点击上方急停",
                $"Playing on the original timeline · input locked · press {StopKeyLabel} or click Stop above");
            color = Theme.Muted;
        }
        else if (_state == RunState.AutoClicking)
        {
            text = L.T(
                $"正在自动连点 · 按 {AutoKeyLabel} 或 {StopKeyLabel} 停止",
                $"Auto clicking · press {AutoKeyLabel} or {StopKeyLabel} to stop");
            color = Theme.Muted;
        }

        if (_state == RunState.Idle &&
            (!StopHotkeyAvailable || !RecordHotkeyAvailable))
        {
            text = L.T("部分快捷键被占用，窗口按钮仍然可用", "Some hotkeys are occupied; window buttons still work");
            color = Theme.Amber;
        }

        SetFooterText(text, color);
    }

    private void SetFooterText(string text, Color color)
    {
        if (_lastFooterText == text && _footerHint.ForeColor == color)
        {
            return;
        }

        _lastFooterText = text;
        _footerHint.Text = text;
        _footerHint.ForeColor = color;
    }

    private void ShowToast(string text, double seconds = 3.5)
    {
        _toastText = text;
        _toastUntil = DateTime.Now.AddSeconds(seconds);
        _footerHint.Text = text;
        _footerHint.ForeColor = Theme.TextSoft;
        _lastFooterText = text;
    }

    private void UpdateUiEnabled()
    {
        var idle = _state == RunState.Idle;
        _playButton.Enabled = idle && _selectedEntry != null;
        _stopButton.Enabled = _state != RunState.Idle;
        _importButton.Enabled = idle;
        _folderButton.Enabled = idle;
        _settingsButton.Enabled = idle;
        _nav.Enabled = idle;

        if (_lastUiIdle != idle)
        {
            _lastUiIdle = idle;
            foreach (var card in _historyCards)
            {
                card.Enabled = idle;
            }
        }

        _speedStepper.Enabled = idle;
        _delaySeg.Enabled = idle;
        _loopStepper.Enabled = idle;
        _recordMovesCheck.Enabled = idle;
        _restoreCursorCheck.Enabled = idle;

        _autoPositionSeg.Enabled = idle;
        _autoButtonSeg.Enabled = idle;
        _autoX.Enabled = idle;
        _autoY.Enabled = idle;
        _autoInterval.Enabled = idle;
        _autoCount.Enabled = idle;
        _autoJitter.Enabled = idle;
        _captureButton.Enabled = idle;
        _autoStartButton.Enabled = idle;
        _autoStopButton.Enabled = _state != RunState.Idle;

        UpdateCoordinateControls();
        _recordCard.Hint = _state == RunState.Recording
            ? L.T(
                $"按 {RecordKeyLabel} 或 {StopKeyLabel} 停止",
                $"Press {RecordKeyLabel} or {StopKeyLabel} to stop")
            : _settings.StartDelaySeconds <= 0
                ? L.T("点击后立即开始", "Starts immediately")
                : L.T(
                    $"{_settings.StartDelaySeconds} 秒倒计时后开始",
                    $"Starts after {_settings.StartDelaySeconds}s");

    }

    private void UpdateCoordinateControls()
    {
        var fixedPosition = _autoPositionSeg.SelectedIndex == 1;
        _autoX.Enabled = _state == RunState.Idle && fixedPosition;
        _autoY.Enabled = _state == RunState.Idle && fixedPosition;
    }

    private void ApplySettingsToUi()
    {
        _suppressUi = true;

        _speedStepper.Value = Math.Clamp(
            (int)Math.Round(_settings.PlaybackSpeed * 100),
            _speedStepper.Min,
            _speedStepper.Max);
        _delaySeg.SelectedIndex = Array.IndexOf(DelayValues, _settings.StartDelaySeconds) is var delayIndex and >= 0
            ? delayIndex
            : 1;
        _loopStepper.Value = Math.Clamp(_settings.LoopCount, 1, 999);
        _recordMovesCheck.Checked = _settings.RecordMouseMoves;
        _restoreCursorCheck.Checked = _settings.RestoreCursor;
        _recorder.RecordMouseMoves = _settings.RecordMouseMoves;

        _autoPositionSeg.SelectedIndex = _settings.AutoUseCurrentPosition ? 0 : 1;
        _autoX.Value = Math.Clamp(_settings.AutoX, _autoX.Min, _autoX.Max);
        _autoY.Value = Math.Clamp(_settings.AutoY, _autoY.Min, _autoY.Max);
        _autoButtonSeg.SelectedIndex = (int)_settings.AutoButton;
        _autoInterval.Value = Math.Clamp(_settings.AutoIntervalMs, _autoInterval.Min, _autoInterval.Max);
        _autoCount.Value = Math.Clamp(_settings.AutoCount, _autoCount.Min, _autoCount.Max);
        _autoJitter.Value = Math.Clamp(_settings.AutoJitterPx, _autoJitter.Min, _autoJitter.Max);

        _suppressUi = false;
        UpdateCoordinateControls();
    }

    private void CollectSettings()
    {
        _settings.PlaybackSpeed = _speedStepper.Value / 100.0;
        _settings.LoopCount = _loopStepper.Value;
        _settings.RecordMouseMoves = _recordMovesCheck.Checked;
        _settings.RestoreCursor = _restoreCursorCheck.Checked;
        _settings.AutoUseCurrentPosition = _autoPositionSeg.SelectedIndex == 0;
        _settings.AutoX = _autoX.Value;
        _settings.AutoY = _autoY.Value;
        _settings.AutoButton = (MouseButtonKind)Math.Clamp(_autoButtonSeg.SelectedIndex, 0, 2);
        _settings.AutoIntervalMs = _autoInterval.Value;
        _settings.AutoCount = _autoCount.Value;
        _settings.AutoJitterPx = _autoJitter.Value;
    }

    private void ClampToWorkingArea()
    {
        var area = Screen.FromControl(this).WorkingArea;
        var bounds = Bounds;

        if (bounds.Width > area.Width)
        {
            Width = area.Width;
        }

        if (bounds.Height > area.Height)
        {
            Height = area.Height;
        }

        bounds = Bounds;
        var x = Math.Clamp(bounds.X, area.Left, Math.Max(area.Left, area.Right - bounds.Width));
        var y = Math.Clamp(bounds.Y, area.Top, Math.Max(area.Top, area.Bottom - bounds.Height));
        if (x != bounds.X || y != bounds.Y)
        {
            Location = new Point(x, y);
        }
    }

    private string StopHintForRecording()
    {
        if (RecordHotkeyAvailable) return L.T($"{RecordKeyLabel} 停止", $"{RecordKeyLabel} stop");
        if (StopHotkeyAvailable) return L.T($"{StopKeyLabel} 停止", $"{StopKeyLabel} stop");
        return L.T("使用窗口按钮停止", "Use the window button to stop");
    }

    private string StopHintForPlayback()
    {
        if (StopHotkeyAvailable) return L.T($"{StopKeyLabel} 停止", $"{StopKeyLabel} stop");
        if (PlayHotkeyAvailable) return L.T($"{PlayKeyLabel} 停止", $"{PlayKeyLabel} stop");
        return L.T("使用窗口按钮停止", "Use the window button to stop");
    }

    private string StopHintForAuto()
    {
        if (StopHotkeyAvailable) return L.T($"{StopKeyLabel} 停止", $"{StopKeyLabel} stop");
        if (AutoHotkeyAvailable) return L.T($"{AutoKeyLabel} 停止", $"{AutoKeyLabel} stop");
        return L.T("使用窗口按钮停止", "Use the window button to stop");
    }

    private void OnEventLimitReached()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            if (_state != RunState.Recording)
            {
                return;
            }

            StopRecording();
            ShowToast(L.T("录制事件已达到 20 万条上限，已自动停止", "Recording reached 200k events and stopped"), 6);
        }));
    }

    private void HandleError(Exception ex)
    {
        var message = ex.Message;
        try
        {
            var dir = MacroStore.AppDataDirectory;
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n",
                Encoding.UTF8);
        }
        catch
        {
            // Logging must never mask the original error.
        }

        RestoreAfterRun();
        if (_closing)
        {
            return;
        }

        MessageDialog.Show(
            this,
            L.T("操作没有完成", "Action did not finish"),
            L.T(
                message + "\n\n如果目标窗口以管理员权限运行，请以管理员身份启动轻录。",
                message + "\n\nIf the target runs as administrator, start ClickFlow as administrator."),
            L.T("知道了", "OK"),
            danger: true);
        ShowToast(L.T("操作失败，详情已写入本地错误日志", "Action failed; details were written to the error log"), 5);
    }
}
