using System.Drawing.Drawing2D;
using ClickFlow.Core;
using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed class SettingsDialog : Form
{
    private readonly AppSettings _settings;
    private readonly List<PaletteSwatch> _swatches = new();
    private readonly SegmentedControl _fontScale;
    private readonly SegmentedControl _startupPage;
    private readonly ModernCheckBox _checkUpdates;
    private readonly Label _updateStatus;
    private readonly FlatButton _checkUpdateButton;
    private readonly FlatButton _releaseButton;
    private readonly Panel _content;
    private ThemePalette _selectedPalette;

    public SettingsDialog(AppSettings settings)
    {
        _settings = settings;
        _selectedPalette = Theme.CurrentPalette;

        SuspendLayout();
        Text = "设置 · 轻录";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(840, 700);
        Padding = new Padding(24, 16, 24, 18);

        var title = new Label
        {
            Text = "设置",
            Font = Theme.Hero,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 40
        };
        var subtitle = new Label
        {
            Text = "外观、通用行为和版本更新都集中在这里。",
            Font = Theme.Body,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 28
        };
        var headerSpacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.Transparent };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderSoft, 1);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var save = new FlatButton
        {
            Text = "保存",
            Style = FlatButtonStyle.Solid,
            AccentColor = Theme.Green,
            Size = new Size(108, 40)
        };
        save.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        var cancel = new FlatButton
        {
            Text = "取消",
            Style = FlatButtonStyle.Ghost,
            Size = new Size(96, 40)
        };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        footer.Controls.Add(save);
        footer.Controls.Add(cancel);
        footer.Resize += (_, _) =>
        {
            var right = footer.ClientSize.Width - 24;
            save.Left = Math.Max(0, right - save.Width);
            cancel.Left = Math.Max(0, save.Left - cancel.Width - 10);
            save.Top = 14;
            cancel.Top = 14;
        };

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 18, 0)
        };
        var content = _content;
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.Controls.Add(flow);
        content.Resize += (_, _) => flow.Width = Math.Max(400, content.ClientSize.Width - 30);

        var appearance = BuildAppearance(out _fontScale);
        var general = BuildGeneral(out _startupPage, out _checkUpdates);
        var data = BuildData();
        var about = BuildAbout(
            out _updateStatus,
            out _checkUpdateButton,
            out _releaseButton);

        flow.Controls.Add(appearance);
        flow.Controls.Add(general);
        flow.Controls.Add(data);
        flow.Controls.Add(about);

        _checkUpdateButton.Click += async (_, _) => await CheckUpdatesAsync();
        _releaseButton.Click += (_, _) => UpdateChecker.OpenReleasePage(
            "https://github.com/yinyouxie/ClickFlow/releases");

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(headerSpacer);
        Controls.Add(subtitle);
        Controls.Add(title);
        ResumeLayout(true);

        _fontScale.SelectedIndex = ScaleIndex(settings.UiScale);
        _startupPage.SelectedIndex = string.Equals(
            settings.StartupPage,
            "auto",
            StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _checkUpdates.Checked = settings.CheckUpdatesOnStartup;
    }

    public ThemePalette SelectedPalette => _selectedPalette;

    public double UiScale => _fontScale.SelectedIndex switch
    {
        0 => 0.94,
        2 => 1.06,
        _ => 1.0
    };

    public string StartupPage => _startupPage.SelectedIndex == 1 ? "auto" : "macro";

    public bool CheckUpdatesOnStartup => _checkUpdates.Checked;

    public bool RestartRequired =>
        Math.Abs(UiScale - Math.Clamp(_settings.UiScale, 0.9, 1.1)) > 0.001;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowEffects.ApplyDarkChrome(this);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ActiveControl = null;
        _content.AutoScrollPosition = Point.Empty;
    }

    private RoundedPanel BuildAppearance(out SegmentedControl fontScale)
    {
        var card = SectionCard(
            "外观",
            "选择背景配色和界面字号。字号会在重启轻录后完全生效。",
            340);

        var swatchFlow = new FlowLayoutPanel
        {
            Location = new Point(18, 74),
            Size = new Size(760, 112),
            BackColor = Color.Transparent,
            WrapContents = true,
            AutoScroll = false,
            Padding = new Padding(0)
        };

        foreach (var palette in Theme.Presets)
        {
            var swatch = new PaletteSwatch(palette)
            {
                Selected = palette.Id == _selectedPalette.Id
            };
            swatch.Click += (_, _) => SelectPalette(palette);
            _swatches.Add(swatch);
            swatchFlow.Controls.Add(swatch);
        }

        card.Controls.Add(swatchFlow);

        var custom = new FlatButton
        {
            Text = "自定义颜色…",
            Style = FlatButtonStyle.Ghost,
            Size = new Size(146, 34),
            Location = new Point(18, 196)
        };
        custom.Click += (_, _) => ChooseCustomColor();
        card.Controls.Add(custom);

        var fontLabel = new Label
        {
            Text = "界面字号",
            Font = Theme.Body,
            ForeColor = Theme.TextSoft,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 266)
        };
        card.Controls.Add(fontLabel);

        fontScale = new SegmentedControl
        {
            Items = new[] { "小", "标准", "大" },
            AccentColor = Theme.Blue,
            Size = new Size(224, 36),
            Location = new Point(104, 254)
        };
        card.Controls.Add(fontScale);

        var fontHint = new Label
        {
            Text = "重启后生效",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(344, 264)
        };
        card.Controls.Add(fontHint);

        return card;
    }

    private RoundedPanel BuildGeneral(
        out SegmentedControl startupPage,
        out ModernCheckBox checkUpdates)
    {
        var card = SectionCard(
            "通用",
            "启动时打开哪个页面，以及是否自动检查新版本。",
            258);

        var languageLabel = new Label
        {
            Text = "界面语言",
            Font = Theme.Body,
            ForeColor = Theme.TextSoft,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 74)
        };
        card.Controls.Add(languageLabel);

        var languageValue = new Label
        {
            Text = "简体中文",
            Font = Theme.BodyBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(142, 74)
        };
        card.Controls.Add(languageValue);

        var languageHint = new Label
        {
            Text = "English 本地化仍在准备中",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(272, 76)
        };
        card.Controls.Add(languageHint);

        var startupLabel = new Label
        {
            Text = "启动页面",
            Font = Theme.Body,
            ForeColor = Theme.TextSoft,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 124)
        };
        card.Controls.Add(startupLabel);

        startupPage = new SegmentedControl
        {
            Items = new[] { "宏录制", "自动连点" },
            AccentColor = Theme.Blue,
            Size = new Size(240, 36),
            Location = new Point(104, 112)
        };
        card.Controls.Add(startupPage);

        checkUpdates = new ModernCheckBox
        {
            Text = "启动时检查更新",
            Size = new Size(260, 26),
            Location = new Point(18, 176),
            AccentColor = Theme.Green,
            Checked = _settings.CheckUpdatesOnStartup
        };
        card.Controls.Add(checkUpdates);

        return card;
    }

    private RoundedPanel BuildData()
    {
        var card = SectionCard(
            "数据",
            "录制记录默认保存在程序旁的 data 文件夹。删除记录会同时删除对应文件。",
            180);

        var path = new Label
        {
            Text = MacroStore.AppDataDirectory,
            Font = Theme.Mono,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = false,
            Location = new Point(18, 78),
            Size = new Size(620, 26),
            AutoEllipsis = true
        };
        card.Controls.Add(path);

        var open = new FlatButton
        {
            Text = "打开数据文件夹",
            Style = FlatButtonStyle.Ghost,
            Size = new Size(164, 36),
            Location = new Point(18, 118)
        };
        open.Click += (_, _) => RecordingLibrary.OpenFolder();
        card.Controls.Add(open);

        var tip = new Label
        {
            Text = "备份或迁移时，复制整个 data 文件夹即可。",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(200, 125)
        };
        card.Controls.Add(tip);

        return card;
    }

    private RoundedPanel BuildAbout(
        out Label updateStatus,
        out FlatButton checkButton,
        out FlatButton releaseButton)
    {
        var card = SectionCard(
            "关于与更新",
            "轻录 ClickFlow · 本地录制与回放工具",
            200);

        var version = new Label
        {
            Text = $"当前版本 v{UpdateChecker.CurrentVersion}",
            Font = Theme.Body,
            ForeColor = Theme.TextSoft,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 78)
        };
        card.Controls.Add(version);

        updateStatus = new Label
        {
            Text = "检查是否有新版本。",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 112)
        };
        card.Controls.Add(updateStatus);

        checkButton = new FlatButton
        {
            Text = "检查更新",
            Style = FlatButtonStyle.Tint,
            AccentColor = Theme.Blue,
            Size = new Size(128, 36),
            Location = new Point(18, 142)
        };
        card.Controls.Add(checkButton);

        releaseButton = new FlatButton
        {
            Text = "打开发布页",
            Style = FlatButtonStyle.Ghost,
            Size = new Size(132, 36),
            Location = new Point(158, 142)
        };
        card.Controls.Add(releaseButton);

        var repo = new Label
        {
            Text = "github.com/yinyouxie/ClickFlow",
            Font = Theme.Mono,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(306, 152)
        };
        card.Controls.Add(repo);

        return card;
    }

    private static RoundedPanel SectionCard(string title, string subtitle, int height)
    {
        var card = new RoundedPanel
        {
            Width = 760,
            Height = height,
            Margin = new Padding(0, 0, 0, 14),
            Radius = 16,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = Theme.LabelBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 14)
        };
        var subtitleLabel = new Label
        {
            Text = subtitle,
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(18, 40)
        };
        card.Controls.Add(titleLabel);
        card.Controls.Add(subtitleLabel);
        return card;
    }

    private void SelectPalette(ThemePalette palette)
    {
        _selectedPalette = palette;
        foreach (var swatch in _swatches)
        {
            swatch.Selected = swatch.Palette.Id == palette.Id;
        }
    }

    private void ChooseCustomColor()
    {
        using var dialog = new ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            Color = _selectedPalette.Bg
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _selectedPalette = Theme.FromBackground("自定义背景", dialog.Color);
        foreach (var swatch in _swatches)
        {
            swatch.Selected = false;
        }
    }

    private async Task CheckUpdatesAsync()
    {
        _checkUpdateButton.Enabled = false;
        _releaseButton.Enabled = false;
        _updateStatus.ForeColor = Theme.Muted;
        _updateStatus.Text = "正在检查…";

        try
        {
            var result = await UpdateChecker.CheckAsync();
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (result.Error != null)
            {
                _updateStatus.ForeColor = Theme.Amber;
                _updateStatus.Text = "检查失败：" + result.Error;
            }
            else if (result.HasUpdate)
            {
                _updateStatus.ForeColor = Theme.Green;
                _updateStatus.Text = $"发现新版本 v{result.LatestVersion}";
            }
            else
            {
                _updateStatus.ForeColor = Theme.Muted;
                _updateStatus.Text = $"已是最新版本 v{result.CurrentVersion}";
            }

            _releaseButton.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            _updateStatus.Text = "检查已取消";
        }
        catch (Exception ex)
        {
            _updateStatus.ForeColor = Theme.Amber;
            _updateStatus.Text = "检查失败：" + ex.Message;
            _releaseButton.Enabled = true;
        }
        finally
        {
            _checkUpdateButton.Enabled = true;
        }
    }

    private static int ScaleIndex(double scale) =>
        scale < 0.97 ? 0 : scale > 1.03 ? 2 : 1;

    private sealed class PaletteSwatch : Control
    {
        public PaletteSwatch(ThemePalette palette)
        {
            Palette = palette;
            Text = palette.Name;
            Size = new Size(178, 48);
            Margin = new Padding(0, 0, 8, 8);
            Cursor = Cursors.Hand;
            Font = Theme.BodySmall;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = palette.Name;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
        }

        public ThemePalette Palette { get; }

        private bool _selected;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value)
                {
                    return;
                }

                _selected = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(1, 1, Width - 2, Height - 2);
            using (var path = Theme.Rounded(rect, 10))
            using (var brush = new SolidBrush(Palette.Bg))
            {
                e.Graphics.FillPath(brush, path);
            }

            Theme.DrawRounded(
                e.Graphics,
                rect,
                10,
                _selected ? Theme.Blue : Theme.Border,
                _selected ? 2f : 1f);

            using (var swatch = new SolidBrush(Palette.Border))
            {
                e.Graphics.FillEllipse(swatch, 12, Height / 2f - 7, 14, 14);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Palette.Name,
                Font,
                new Rectangle(34, 0, Width - 46, Height),
                Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            if (_selected)
            {
                Theme.DrawGlyph(
                    e.Graphics,
                    Glyph.Check,
                    new RectangleF(Width - 24, Height / 2f - 7, 14, 14),
                    Theme.Blue,
                    2f);
            }
        }
    }
}
