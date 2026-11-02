using ClickFlow.Core;
using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed partial class MainForm
{
    private void WireEvents()
    {
        _recordCard.Click += (_, _) => RunSafe(ToggleRecordingAsync);
        _playButton.Click += (_, _) => RunSafe(TogglePlaybackAsync);
        _stopButton.Click += (_, _) => StopEverything();
        _nav.SelectedIndexChanged += (_, _) => OnNavigationChanged();
        _helpButton.Click += (_, _) => ShowHelp();
        _settingsButton.Click += (_, _) => ShowSettings();
        _importButton.Click += (_, _) => ImportMacroDialog();
        _folderButton.Click += (_, _) => RecordingLibrary.OpenFolder();
        _historyFlow.Resize += (_, _) => ResizeHistoryCards();
        _inputGuard.StopRequested += () =>
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            try
            {
                BeginInvoke(new Action(StopEverything));
            }
            catch
            {
                // The form can be closing while the guard is being torn down.
            }
        };

        _speedStepper.ValueChanged += (_, _) =>
        {
            if (_suppressUi)
            {
                return;
            }

            _settings.PlaybackSpeed = _speedStepper.Value / 100.0;
            ShowToast(L.T(
                $"回放速度：{_settings.PlaybackSpeed:0.##}×",
                $"Playback speed: {_settings.PlaybackSpeed:0.##}×"), 1.6);
        };
        _delaySeg.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUi)
            {
                return;
            }

            _settings.StartDelaySeconds = DelayValues[Math.Clamp(_delaySeg.SelectedIndex, 0, DelayValues.Length - 1)];
            UpdateUiEnabled();
        };
        _loopStepper.ValueChanged += (_, _) =>
        {
            if (!_suppressUi)
            {
                _settings.LoopCount = _loopStepper.Value;
            }
        };
        _recordMovesCheck.CheckedChanged += (_, _) =>
        {
            if (_suppressUi)
            {
                return;
            }

            _settings.RecordMouseMoves = _recordMovesCheck.Checked;
            _recorder.RecordMouseMoves = _recordMovesCheck.Checked;
        };
        _restoreCursorCheck.CheckedChanged += (_, _) =>
        {
            if (!_suppressUi)
            {
                _settings.RestoreCursor = _restoreCursorCheck.Checked;
            }
        };

        _autoPositionSeg.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUi)
            {
                return;
            }

            _settings.AutoUseCurrentPosition = _autoPositionSeg.SelectedIndex == 0;
            UpdateCoordinateControls();
            RefreshShell();
        };
        _autoButtonSeg.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppressUi)
            {
                _settings.AutoButton = (MouseButtonKind)Math.Clamp(_autoButtonSeg.SelectedIndex, 0, 2);
            }
        };
        _autoX.ValueChanged += (_, _) => AutoSettingChanged();
        _autoY.ValueChanged += (_, _) => AutoSettingChanged();
        _autoInterval.ValueChanged += (_, _) => AutoSettingChanged();
        _autoCount.ValueChanged += (_, _) => AutoSettingChanged();
        _autoJitter.ValueChanged += (_, _) => AutoSettingChanged();
        _captureButton.Click += (_, _) => CaptureCursorPosition(switchToAuto: false);
        _autoStartButton.Click += (_, _) => RunSafe(ToggleAutoAsync);
        _autoStopButton.Click += (_, _) => StopEverything();

        DragEnter += OnFileDragEnter;
        DragDrop += OnFileDragDrop;

        _toolTip.SetToolTip(_helpButton, L.T("使用帮助 (Ctrl+H)", "Help (Ctrl+H)"));
        _toolTip.SetToolTip(_settingsButton, L.T("设置", "Settings"));
        _toolTip.SetToolTip(_importButton, L.T("把外部 .cfmacro 导入录制历史 (Ctrl+O)", "Import an external .cfmacro (Ctrl+O)"));
        _toolTip.SetToolTip(_folderButton, L.T("打开录制文件所在文件夹", "Open the recordings folder"));
        _toolTip.SetToolTip(_captureButton, L.T("读取当前鼠标坐标；也可以在任意窗口按 F5", "Read the current cursor position; F5 works anywhere"));
        _toolTip.SetToolTip(_speedStepper, L.T("每次 ±0.25×；也可以点击数字直接输入", "Steps by 0.25×; click the number to type"));
        _toolTip.SetToolTip(_loopStepper, L.T("点击数字直接输入循环次数，范围 1–999", "Click the number to type loops (1–999)"));
        _toolTip.SetToolTip(_recordCard, L.T("开始或停止录制 (F1)", "Start or stop recording (F1)"));
        _toolTip.SetToolTip(_playButton, L.T("回放选中的录制 (F2)", "Replay the selected recording (F2)"));
        _toolTip.SetToolTip(_stopButton, L.T("立即停止录制、回放或连点 (F3)", "Stop recording, playback or auto click (F3)"));
    }

    private void UpdateHotkeyLabels()
    {
        if (_recordCard == null || _stopHintLabel == null)
        {
            return;
        }

        _recordCard.HotkeyLabel = RecordKeyLabel;
        _playButton.Hotkey = PlayHotkeyAvailable ? PlayKeyLabel : string.Empty;
        _stopButton.Hotkey = StopHotkeyAvailable ? StopKeyLabel : string.Empty;
        _autoStartButton.Hotkey = AutoHotkeyAvailable ? AutoKeyLabel : string.Empty;
        _autoStopButton.Hotkey = StopHotkeyAvailable ? StopKeyLabel : string.Empty;

        _stopHintLabel.Text = StopHotkeyAvailable
            ? L.T($"{StopKeyLabel} 急停", $"{StopKeyLabel} stop")
            : L.T("急停", "Stop");
        _captureHintLabel.Text = _hotkeyCapture || _hotkeyCaptureAlt
            ? L.T($"{CaptureKeyLabel} 取坐标", $"{CaptureKeyLabel} pick")
            : L.T("取坐标", "Pick");
        _autoTipLabel.Text = L.T(
            $"{AutoKeyLabel} 连点  ·  {CaptureKeyLabel} 取坐标  ·  {StopKeyLabel} 急停\n" +
            "次数 0 = 无限，直到手动停止。",
            $"{AutoKeyLabel} click · {CaptureKeyLabel} pick · {StopKeyLabel} stop\nCount 0 = unlimited until stopped.");
        _autoFooterHint.Text = L.T(
            $"开始前短暂准备，让目标窗口获得焦点；{StopKeyLabel} 可随时停止。",
            $"Brief countdown to focus the target; {StopKeyLabel} stops at any time.");
        _historyEmpty.Text = L.T(
            $"还没有录制记录\n\n按 {RecordKeyLabel} 开始录制，停止后会自动保存在这里",
            $"No recordings yet\n\nPress {RecordKeyLabel} to record; it will be saved here.");

        if (_stopHintLabel.Parent != null)
        {
            _stopHintLabel.Left = _stopHintLabel.Parent.ClientSize.Width - _stopHintLabel.Width;
        }

        if (_captureHintLabel.Parent != null)
        {
            _captureHintLabel.Left = _captureHintLabel.Parent.ClientSize.Width - _captureHintLabel.Width;
        }

        _toolTip.SetToolTip(_recordCard, L.T($"开始或停止录制 ({RecordKeyLabel})", $"Start or stop recording ({RecordKeyLabel})"));
        _toolTip.SetToolTip(_playButton, L.T($"回放选中的录制 ({PlayKeyLabel})", $"Replay the selected recording ({PlayKeyLabel})"));
        _toolTip.SetToolTip(_stopButton, L.T($"立即停止录制、回放或连点 ({StopKeyLabel})", $"Stop recording, playback or auto click ({StopKeyLabel})"));
        _toolTip.SetToolTip(_captureButton, L.T($"读取当前鼠标坐标；也可以在任意窗口按 {CaptureKeyLabel}", $"Read the cursor position; {CaptureKeyLabel} works anywhere"));

        UpdateFooter();
    }

    private void AutoSettingChanged()
    {
        if (_suppressUi)
        {
            return;
        }

        _settings.AutoX = _autoX.Value;
        _settings.AutoY = _autoY.Value;
        _settings.AutoIntervalMs = _autoInterval.Value;
        _settings.AutoCount = _autoCount.Value;
        _settings.AutoJitterPx = _autoJitter.Value;
        RefreshShell();
    }

    private void OnNavigationChanged()
    {
        var requested = (AppMode)Math.Clamp(_nav.SelectedIndex, 0, 1);
        if (_state != RunState.Idle)
        {
            _suppressUi = true;
            _nav.SelectedIndex = (int)_mode;
            _suppressUi = false;
            ShowToast(L.T("请先停止正在进行的操作", "Stop the running action first"), 2);
            return;
        }

        SetMode(requested);
    }

    private void SwitchMode(AppMode mode)
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止正在进行的操作", "Stop the running action first"), 2);
            return;
        }

        SetMode(mode);
    }

    private void SetMode(AppMode mode)
    {
        _mode = mode;
        _suppressUi = true;
        _nav.SelectedIndex = (int)mode;
        _suppressUi = false;

        var macro = mode == AppMode.Macro;
        _leftMacro.Visible = macro;
        _rightMacro.Visible = macro;
        _leftAuto.Visible = !macro;
        _rightAuto.Visible = !macro;
        _body.PerformLayout();
        UpdateFooter();
        RefreshShell();
    }

    private async Task ToggleRecordingAsync()
    {
        if (_state == RunState.Recording)
        {
            StopRecording();
            return;
        }

        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        await StartRecordingAsync();
    }

    private async Task StartRecordingAsync()
    {
        _cts = new CancellationTokenSource();
        var wasForeground = IsOurAppForeground();
        SetState(RunState.Preparing);
        try
        {
            var proceed = await CountdownOverlay.RunAsync(
                this,
                _settings.StartDelaySeconds,
                L.T("准备录制", "Preparing to record"),
                Theme.Accent,
                _cts.Token);

            if (!proceed)
            {
                return;
            }

            await PrepareForRunAsync(RunState.Recording, wasForeground, _cts.Token);
            _cts.Token.ThrowIfCancellationRequested();

            _targetTitle = GetForegroundWindowTitle();
            _document = new MacroDocument();

            _recorder.RecordMouseMoves = _recordMovesCheck.Checked;
            if (!_recorder.Start(null, out var error))
            {
                RestoreAfterRun();
                MessageDialog.Show(
                    this,
                    L.T("无法开始录制", "Cannot start recording"),
                    L.T(
                        error + "\n\n请确认系统允许轻录监听全局输入。",
                        error + "\n\nMake sure the system allows ClickFlow to monitor global input."),
                    L.T("知道了", "OK"),
                    danger: true);
                return;
            }

            SetState(RunState.Recording);
            _cts.Dispose();
            _cts = null;
            ShowToast(L.T($"录制已开始 · 按 {RecordKeyLabel} 停止", $"Recording · press {RecordKeyLabel} to stop"), 2.5);
        }
        catch (OperationCanceledException)
        {
            ShowToast(L.T("已取消录制", "Recording cancelled"), 2);
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            if (_state != RunState.Recording)
            {
                _cts?.Dispose();
                _cts = null;
                SetState(RunState.Idle);
                RestoreAfterRun();
            }
        }
    }

    private void StopRecording()
    {
        if (_state != RunState.Recording)
        {
            return;
        }

        try
        {
            var events = _recorder.Stop();
            RecordingEntry? entry = null;
            _document = BuildDocument(events);

            if (events.Count > 0)
            {
                try
                {
                    entry = RecordingLibrary.Add(_document);
                }
                catch (Exception archiveError)
                {
                    ShowToast(L.T("录制完成，但保存到历史失败：", "Recorded, but saving to History failed: ") + archiveError.Message, 6);
                }
            }

            SetState(RunState.Idle);
            RestoreAfterRun();

            if (events.Count == 0)
            {
                ShowToast(L.T("没有录到操作，请再试一次", "No input captured; try again"), 3.5);
            }
            else
            {
                RefreshHistory(entry?.Id);
                SelectEntry(entry);
                ShowToast(
                    L.T($"录制已保存 · {entry?.ActionCount ?? 0} 个操作 · ", $"Saved · {entry?.ActionCount ?? 0} actions · ") +
                    $"{Theme.FormatDuration(_document.DurationMs)}",
                    4);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task TogglePlaybackAsync()
    {
        if (_state == RunState.Playing)
        {
            StopEverything();
            return;
        }

        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        if (_selectedEntry == null)
        {
            ShowToast(L.T(
                $"请先在右侧选中一条录制，再按 {PlayKeyLabel} 回放",
                $"Select a recording on the right, then press {PlayKeyLabel} to replay"), 3);
            return;
        }

        await PlayEntryAsync(_selectedEntry);
    }

    private async Task PlayEntryAsync(RecordingEntry entry)
    {
        MacroDocument document;
        try
        {
            document = RecordingLibrary.LoadDocument(entry);
        }
        catch (Exception ex)
        {
            MessageDialog.Show(
                this,
                L.T("无法读取录制", "Cannot read recording"),
                ex.Message,
                L.T("知道了", "OK"),
                danger: true);
            return;
        }

        if (document.Events.Count == 0)
        {
            ShowToast(L.T("这条录制没有可回放的操作", "This recording has no replayable actions"), 2.5);
            return;
        }

        _document = document;
        _cts = new CancellationTokenSource();
        var wasForeground = IsOurAppForeground();
        SetState(RunState.Preparing);

        try
        {
            var proceed = await CountdownOverlay.RunAsync(
                this,
                _settings.StartDelaySeconds,
                L.T("准备回放", "Preparing playback"),
                Theme.Green,
                _cts.Token);
            if (!proceed)
            {
                return;
            }

            await PrepareForRunAsync(RunState.Playing, wasForeground, _cts.Token);
            _cts.Token.ThrowIfCancellationRequested();

            _playbackPercent = 0;
            _playbackLoop = 1;
            _playbackLoops = Math.Clamp(_loopStepper.Value, 1, 999);
            SetState(RunState.Playing);
            RefreshShell();

            if (!StartPlaybackInputGuard(out var guardError))
            {
                throw new InvalidOperationException(L.T(
                    guardError + "\n\n为避免回放被手动输入干扰，本次回放已取消。",
                    guardError + "\n\nPlayback was cancelled to avoid interference from manual input."));
            }

            var options = new PlaybackOptions
            {
                Speed = _settings.PlaybackSpeed,
                Loops = _playbackLoops,
                RestoreCursor = _restoreCursorCheck.Checked,
                LoopGapMs = 260
            };
            var progress = new Progress<PlaybackProgress>(OnPlaybackProgress);
            _operationTask = _player.PlayAsync(_document, options, progress, _cts.Token);
            ShowToast(L.T(
                $"回放中 · 输入已锁定，按 {StopKeyLabel} 或点击上方急停",
                $"Playing · input locked · press {StopKeyLabel} or click Stop above"), 2.5);
            await _operationTask;

            if (_state == RunState.Playing)
            {
                ShowToast(L.T(
                    $"回放完成 · 共 {_playbackLoops} 轮",
                    $"Playback complete · {_playbackLoops} loops"), 3.5);
            }
        }
        catch (OperationCanceledException)
        {
            _inputGuard.Stop();
            ShowToast(L.T("回放已停止", "Playback stopped"), 2.5);
        }
        catch (Exception ex)
        {
            _inputGuard.Stop();
            HandleError(ex);
        }
        finally
        {
            _inputGuard.Stop();
            _operationTask = null;
            _cts?.Dispose();
            _cts = null;
            if (_state != RunState.Idle)
            {
                SetState(RunState.Idle);
            }

            RestoreAfterRun();
        }
    }

    private bool StartPlaybackInputGuard(out string error)
    {
        var areas = new List<Rectangle>();
        if (_hud != null && _hud.Visible)
        {
            areas.Add(_hud.Bounds);
        }

        if (_stopButton.Visible && WindowState != FormWindowState.Minimized)
        {
            areas.Add(_stopButton.RectangleToScreen(_stopButton.ClientRectangle));
        }

        return _inputGuard.Start(areas, () => _player.IsPointerDown, out error);
    }

    private void OnPlaybackProgress(PlaybackProgress progress)
    {
        if (_state != RunState.Playing)
        {
            return;
        }

        _playbackPercent = (long)Math.Round(progress.Percent);
        _playbackLoop = progress.Loop;
        _playbackLoops = progress.Loops;
        RefreshShell();
    }

    private async Task ToggleAutoAsync()
    {
        if (_state == RunState.AutoClicking)
        {
            StopEverything();
            return;
        }

        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        var options = BuildAutoOptions();
        _cts = new CancellationTokenSource();
        SetState(RunState.Preparing);

        try
        {
            var foreground = IsOurAppForeground();
            var canMinimize = CanMinimizeSafely(RunState.AutoClicking);

            if (foreground && options.UseCurrentPosition && !canMinimize)
            {
                ShowToast(L.T(
                    $"请先切换到目标窗口再按 {AutoKeyLabel}",
                    $"Switch to the target window, then press {AutoKeyLabel}"), 4);
                return;
            }

            if (foreground && canMinimize)
            {
                _minimizedForRun = true;
                WindowState = FormWindowState.Minimized;
            }

            ShowToast(L.T(
                $"连点准备中 · 按 {StopKeyLabel} 取消",
                $"Preparing auto click · press {StopKeyLabel} to cancel"), 2);
            await Task.Delay(foreground ? 700 : 220, _cts.Token);
            _cts.Token.ThrowIfCancellationRequested();

            _autoClicker.ResetCount();
            _autoPulse.Count = 0;
            SetState(RunState.AutoClicking);
            _operationTask = _autoClicker.RunAsync(options, null, _cts.Token);
            await _operationTask;

            if (_state == RunState.AutoClicking)
            {
                var count = _autoClicker.Count;
                ShowToast(
                    count > 0
                        ? L.T($"连点完成 · {count:N0} 次", $"Auto click complete · {count:N0}")
                        : L.T("连点已结束", "Auto click finished"),
                    3);
            }
        }
        catch (OperationCanceledException)
        {
            ShowToast(L.T("连点已停止", "Auto click stopped"), 2.5);
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
        finally
        {
            _operationTask = null;
            _cts?.Dispose();
            _cts = null;
            if (_state != RunState.Idle)
            {
                SetState(RunState.Idle);
            }

            RestoreAfterRun();
        }
    }

    private AutoClickOptions BuildAutoOptions()
    {
        _settings.AutoUseCurrentPosition = _autoPositionSeg.SelectedIndex == 0;
        _settings.AutoX = _autoX.Value;
        _settings.AutoY = _autoY.Value;
        _settings.AutoButton = (MouseButtonKind)Math.Clamp(_autoButtonSeg.SelectedIndex, 0, 2);
        _settings.AutoIntervalMs = _autoInterval.Value;
        _settings.AutoCount = _autoCount.Value;
        _settings.AutoJitterPx = _autoJitter.Value;

        return new AutoClickOptions
        {
            UseCurrentPosition = _settings.AutoUseCurrentPosition,
            X = _settings.AutoX,
            Y = _settings.AutoY,
            Button = _settings.AutoButton,
            IntervalMs = _settings.AutoIntervalMs,
            ClickCount = _settings.AutoCount,
            JitterPx = _settings.AutoJitterPx
        };
    }

    private void StopEverything()
    {
        switch (_state)
        {
            case RunState.Recording:
                StopRecording();
                return;
            case RunState.Idle:
                ShowToast(L.T("当前没有进行中的操作", "Nothing is running"), 1.6);
                return;
            case RunState.Preparing:
            case RunState.Playing:
            case RunState.AutoClicking:
                if (_cts != null)
                {
                    _cts.Cancel();
                    ShowToast(L.T("正在停止…", "Stopping…"), 1.8);
                }
                else
                {
                    SetState(RunState.Idle);
                    RestoreAfterRun();
                }

                return;
        }
    }

    private void CaptureCursorPosition(bool switchToAuto)
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        var point = Cursor.Position;
        _suppressUi = true;
        _autoPositionSeg.SelectedIndex = 1;
        _autoX.Value = Math.Clamp(point.X, _autoX.Min, _autoX.Max);
        _autoY.Value = Math.Clamp(point.Y, _autoY.Min, _autoY.Max);
        _suppressUi = false;

        _settings.AutoUseCurrentPosition = false;
        _settings.AutoX = _autoX.Value;
        _settings.AutoY = _autoY.Value;
        UpdateCoordinateControls();

        if (switchToAuto)
        {
            SwitchMode(AppMode.Auto);
        }

        RefreshShell();
        ShowToast(L.T(
            $"已捕获坐标 ({_autoX.Value}, {_autoY.Value})",
            $"Captured position ({_autoX.Value}, {_autoY.Value})"), 2.5);
    }

    private async Task PrepareForRunAsync(
        RunState intended,
        bool wasForeground,
        CancellationToken cancellationToken)
    {
        if (!wasForeground && !IsOurAppForeground())
        {
            return;
        }

        if (!CanMinimizeSafely(intended))
        {
            ShowToast(L.T(
                "全局停止快捷键被占用，本次不会自动最小化",
                "Global stop hotkey is occupied; the window will not minimize"), 3);
            return;
        }

        _minimizedForRun = true;
        WindowState = FormWindowState.Minimized;
        await Task.Delay(320, cancellationToken);
    }

    private void RestoreAfterRun()
    {
        if (!_minimizedForRun)
        {
            return;
        }

        _minimizedForRun = false;
        if (_closing || IsDisposed)
        {
            return;
        }

        WindowState = FormWindowState.Normal;
        Show();
        Activate();
        NativeMethods.SetForegroundWindow(Handle);
    }

    private bool CanMinimizeSafely(RunState intended)
    {
        if (StopHotkeyAvailable)
        {
            return true;
        }

        return intended switch
        {
            RunState.Recording => RecordHotkeyAvailable,
            RunState.Playing => PlayHotkeyAvailable,
            RunState.AutoClicking => AutoHotkeyAvailable,
            _ => false
        };
    }

    private bool IsOurAppForeground()
    {
        return NativeMethods.GetForegroundWindow() == Handle || ContainsFocus;
    }

    private MacroDocument BuildDocument(List<MacroEvent> events)
    {
        var screen = SystemInformation.VirtualScreen;
        var name = string.IsNullOrWhiteSpace(_targetTitle)
            ? L.T(
                $"录制 {DateTime.Now:MM-dd HH:mm}",
                $"Recording {DateTime.Now:MM-dd HH:mm}")
            : _targetTitle;

        return new MacroDocument
        {
            Name = name,
            CreatedUtc = DateTime.UtcNow,
            AppVersion = "1.2.0",
            ScreenWidth = screen.Width,
            ScreenHeight = screen.Height,
            Events = events
        };
    }

    private static string GetForegroundWindowTitle()
    {
        try
        {
            var handle = NativeMethods.GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return string.Empty;
            }

            var length = NativeMethods.GetWindowTextLength(handle);
            if (length <= 0)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(length + 2);
            NativeMethods.GetWindowText(handle, builder, builder.Capacity);
            var title = builder.ToString().Trim();
            if (title.Length == 0 ||
                title.Equals("轻录 ClickFlow", StringComparison.OrdinalIgnoreCase) ||
                title.Equals("ClickFlow", StringComparison.OrdinalIgnoreCase) ||
                title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return title.Length <= 80 ? title : title[..77] + "…";
        }
        catch
        {
            return string.Empty;
        }
    }

    private void ShowHelp()
    {
        if (_state != RunState.Idle)
        {
            ShowToast(L.T("请先停止当前操作", "Stop the current action first"), 2);
            return;
        }

        using var dialog = new HelpDialog();
        dialog.ShowDialog(this);
    }

}
