using ClickFlow.Core;

namespace ClickFlow.UI;

internal sealed partial class MainForm
{
    private void ShowSettings()
    {
        if (_state != RunState.Idle)
        {
            ShowToast("请先停止当前操作", 2);
            return;
        }

        using var dialog = new SettingsDialog(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var restartRequired = dialog.RestartRequired;
        var paletteChanged = !string.Equals(
            dialog.SelectedPalette.Id,
            Theme.CurrentPalette.Id,
            StringComparison.OrdinalIgnoreCase) ||
            (dialog.SelectedPalette.Id == "custom" &&
             dialog.SelectedPalette.Bg.ToArgb() != Theme.CurrentPalette.Bg.ToArgb());

        _settings.UiScale = Math.Clamp(dialog.UiScale, 0.9, 1.1);
        _settings.StartupPage = dialog.StartupPage;
        _settings.CheckUpdatesOnStartup = dialog.CheckUpdatesOnStartup;
        _settings.RecordMouseMoves = dialog.RecordMouseMoves;
        _settings.RestoreCursor = dialog.RestoreCursor;
        _settings.Language = "zh-CN";
        MacroStore.SaveSettings(_settings);
        ApplySettingsToUi();

        if (paletteChanged)
        {
            _settings.ThemeId = dialog.SelectedPalette.Id;
            _settings.CustomThemeArgb = dialog.SelectedPalette.Id == "custom"
                ? dialog.SelectedPalette.Bg.ToArgb()
                : 0;
            ApplyTheme(dialog.SelectedPalette);
        }
        else
        {
            ApplySettingsToUi();
            RefreshShell();
            ShowToast("设置已保存", 2);
        }

        if (restartRequired)
        {
            MessageDialog.Show(
                this,
                "需要重启",
                "字号设置已保存。\n\n请关闭轻录后重新打开，新字号才会生效。",
                "知道了",
                null,
                danger: false);
        }
    }

    private async Task CheckUpdatesOnStartupAsync()
    {
        try
        {
            var result = await UpdateChecker.CheckAsync();
            if (_closing || IsDisposed || !result.HasUpdate)
            {
                return;
            }

            BeginInvoke(() =>
            {
                if (!_closing && !IsDisposed)
                {
                    ShowToast($"发现新版本 v{result.LatestVersion} · 点击设置查看", 6);
                }
            });
        }
        catch
        {
            // A startup update check must never interrupt the user.
        }
    }
}
