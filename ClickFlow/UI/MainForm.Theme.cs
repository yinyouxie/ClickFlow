using ClickFlow.Core;

namespace ClickFlow.UI;

internal sealed partial class MainForm
{
    private void ApplyTheme(ThemePalette palette)
    {
        if (_state != RunState.Idle)
        {
            ShowToast("请先停止当前操作", 2);
            return;
        }

        var previous = Theme.CurrentPalette;
        Theme.Apply(palette);
        BackColor = Theme.Bg;
        Theme.RecolorControlTree(this, previous, palette);

        _settings.ThemeId = palette.Id;
        _settings.CustomThemeArgb = palette.Id == "custom" ? palette.Bg.ToArgb() : 0;
        MacroStore.SaveSettings(_settings);

        Invalidate(true);
        Refresh();
        RefreshShell();
        ShowToast($"背景已切换 · {palette.Name}", 2.5);
    }
}
