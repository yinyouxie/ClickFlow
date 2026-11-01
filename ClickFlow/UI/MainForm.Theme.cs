using System.Drawing.Drawing2D;
using ClickFlow.Core;

namespace ClickFlow.UI;

internal sealed partial class MainForm
{
    private void ShowThemeMenu()
    {
        if (_state != RunState.Idle)
        {
            ShowToast("请先停止当前操作", 2);
            return;
        }

        var menu = new ContextMenuStrip
        {
            BackColor = Theme.SurfaceRaised,
            ForeColor = Theme.Text,
            Font = Theme.Body,
            ShowImageMargin = true,
            ShowCheckMargin = true,
            Padding = new Padding(4),
            Renderer = new ToolStripProfessionalRenderer(new ThemeMenuColorTable())
        };

        foreach (var palette in Theme.Presets)
        {
            var item = new ToolStripMenuItem(palette.Name)
            {
                Image = CreateSwatch(palette.Bg),
                Checked = Theme.CurrentPalette.Id == palette.Id,
                ForeColor = Theme.Text
            };
            item.Click += (_, _) =>
            {
                menu.Close();
                ApplyTheme(palette);
            };
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());
        var custom = new ToolStripMenuItem("自定义背景…")
        {
            Image = CreateSwatch(Theme.Bg),
            ForeColor = Theme.Text
        };
        custom.Click += (_, _) =>
        {
            menu.Close();
            ChooseCustomBackground();
        };
        menu.Items.Add(custom);
        menu.Closed += (_, _) => menu.Dispose();
        menu.Show(_themeButton, new Point(0, _themeButton.Height + 4));
    }

    private void ChooseCustomBackground()
    {
        using var dialog = new ColorDialog
        {
            FullOpen = true,
            Color = Theme.Bg,
            AnyColor = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var palette = Theme.FromBackground("自定义背景", dialog.Color);
        ApplyTheme(palette);
        if (palette.Bg.ToArgb() != dialog.Color.ToArgb())
        {
            ShowToast("已自动压暗背景，保证文字清晰", 3);
        }
    }

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

    private static Image CreateSwatch(Color color)
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var path = Theme.Rounded(new RectangleF(1, 1, 16, 16), 4);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
        using var pen = new Pen(Theme.Border, 1);
        graphics.DrawPath(pen, path);
        return bitmap;
    }

    private sealed class ThemeMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.SurfaceRaised;
        public override Color ImageMarginGradientBegin => Theme.SurfaceRaised;
        public override Color ImageMarginGradientMiddle => Theme.SurfaceRaised;
        public override Color ImageMarginGradientEnd => Theme.SurfaceRaised;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Theme.Blue;
        public override Color MenuItemSelected => Theme.SurfaceHover;
        public override Color MenuItemSelectedGradientBegin => Theme.SurfaceHover;
        public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;
        public override Color SeparatorDark => Theme.BorderSoft;
        public override Color SeparatorLight => Theme.SurfaceRaised;
        public override Color CheckBackground => Theme.SurfaceHover;
        public override Color CheckSelectedBackground => Theme.SurfaceHover;
    }
}
