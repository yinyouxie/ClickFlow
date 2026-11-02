using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed partial class MainForm
{
    private TableLayoutPanel _root = null!;
    private Panel _header = null!;
    private SegmentedControl _nav = null!;
    private StatusPill _statusPill = null!;
    private FlatButton _helpButton = null!;
    private FlatButton _settingsButton = null!;
    private TableLayoutPanel _body = null!;
    private TableLayoutPanel _leftMacro = null!;
    private TableLayoutPanel _leftAuto = null!;
    private TableLayoutPanel _rightMacro = null!;
    private RoundedPanel _rightAuto = null!;
    private Panel _footer = null!;

    private RecordCard _recordCard = null!;
    private FlatButton _playButton = null!;
    private FlatButton _stopButton = null!;
    private NumberStepper _speedStepper = null!;
    private SegmentedControl _delaySeg = null!;
    private NumberStepper _loopStepper = null!;
    private ModernCheckBox _recordMovesCheck = null!;
    private ModernCheckBox _restoreCursorCheck = null!;
    private FlowLayoutPanel _historyFlow = null!;
    private Label _historyCount = null!;
    private Label _historySize = null!;
    private Label _historyEmpty = null!;
    private FlatButton _importButton = null!;
    private FlatButton _folderButton = null!;
    private Label _stopHintLabel = null!;
    private Label _captureHintLabel = null!;
    private Label _autoTipLabel = null!;
    private Label _autoFooterHint = null!;

    private SegmentedControl _autoPositionSeg = null!;
    private SegmentedControl _autoButtonSeg = null!;
    private NumberStepper _autoX = null!;
    private NumberStepper _autoY = null!;
    private NumberStepper _autoInterval = null!;
    private NumberStepper _autoCount = null!;
    private NumberStepper _autoJitter = null!;
    private FlatButton _captureButton = null!;
    private FlatButton _autoStartButton = null!;
    private FlatButton _autoStopButton = null!;
    private AutoPulseView _autoPulse = null!;
    private Label _autoStatusLabel = null!;

    private Label _footerHint = null!;
    private Label _footerVersion = null!;

    private void BuildUi()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        BackColor = Theme.Bg;
        BackgroundImageLayout = ImageLayout.Stretch;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        Text = "轻录 ClickFlow";
        ClientSize = new Size(1220, 800);
        MinimumSize = new Size(1020, 640);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        KeyPreview = true;
        AllowDrop = true;
        Padding = new Padding(0);

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 8, 24, 6)
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        _header = BuildHeader();
        _body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 6)
        };
        _body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 332));
        _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _leftMacro = BuildLeftMacro();
        _leftAuto = BuildLeftAuto();
        _rightMacro = BuildRightMacro();
        _rightAuto = BuildRightAuto();

        _leftMacro.Margin = new Padding(0, 0, 18, 0);
        _leftAuto.Margin = new Padding(0, 0, 18, 0);

        _body.Controls.Add(_leftMacro, 0, 0);
        _body.Controls.Add(_leftAuto, 0, 0);
        _body.Controls.Add(_rightMacro, 1, 0);
        _body.Controls.Add(_rightAuto, 1, 0);

        _footer = BuildFooter();

        _root.Controls.Add(_header, 0, 0);
        _root.Controls.Add(_body, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        Controls.Add(_root);

        _leftAuto.Visible = false;
        _rightAuto.Visible = false;
        _rightMacro.Visible = true;
        _leftMacro.Visible = true;

        ResumeLayout(true);
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };

        var logo = new LogoMark { Location = new Point(0, 7), Size = new Size(42, 42) };
        var title = new Label
        {
            Text = "轻录",
            Font = new Font("Microsoft YaHei UI", 15.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(53, 1)
        };
        var subtitle = new Label
        {
            Text = "ClickFlow  ·  把重复操作录一次",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(55, 32)
        };

        _nav = new SegmentedControl
        {
            Items = new[] { "宏录制", "自动连点" },
            SelectedIndex = 0,
            Size = new Size(208, 36),
            AccentColor = Theme.Blue,
            SelectedTextColor = Theme.Text,
            ShowAccentBar = true,
            Font = Theme.BodyBold
        };

        _statusPill = new StatusPill
        {
            Size = new Size(160, 30),
            AutoWidth = false,
            Text = "待机",
            DotColor = Theme.Green
        };

        _settingsButton = new FlatButton
        {
            Text = string.Empty,
            Icon = Glyph.Settings,
            Style = FlatButtonStyle.Ghost,
            Size = new Size(34, 34),
            Radius = 9
        };

        _helpButton = new FlatButton
        {
            Text = string.Empty,
            Icon = Glyph.Help,
            Style = FlatButtonStyle.Ghost,
            Size = new Size(34, 34),
            Radius = 9
        };

        var divider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Theme.BorderSoft
        };

        header.Controls.Add(logo);
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(_nav);
        header.Controls.Add(_statusPill);
        header.Controls.Add(_settingsButton);
        header.Controls.Add(_helpButton);
        header.Controls.Add(divider);

        header.Resize += (_, _) => LayoutHeader();
        return header;
    }

    private void LayoutHeader()
    {
        if (_header.Width <= 0)
        {
            return;
        }

        _nav.Left = Math.Max(310, (_header.Width - _nav.Width) / 2);
        _nav.Top = 14;
        _helpButton.Left = _header.Width - _helpButton.Width;
        _helpButton.Top = 14;
        _settingsButton.Left = _helpButton.Left - _settingsButton.Width - 8;
        _settingsButton.Top = 14;
        _statusPill.Left = _settingsButton.Left - _statusPill.Width - 12;
        _statusPill.Top = 17;
    }

    private TableLayoutPanel BuildLeftMacro()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 5
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _recordCard = new RecordCard { Dock = DockStyle.Fill };

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        _playButton = new FlatButton
        {
            Text = "回放选中",
            Icon = Glyph.Play,
            Hotkey = "F2",
            Style = FlatButtonStyle.Tint,
            AccentColor = Theme.Green,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 4, 0)
        };
        _stopButton = new FlatButton
        {
            Text = "停止",
            Icon = Glyph.Stop,
            Hotkey = "F3",
            Style = FlatButtonStyle.Ghost,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0)
        };
        actions.Controls.Add(_playButton, 0, 0);
        actions.Controls.Add(_stopButton, 1, 0);

        var settings = BuildSettingsCard();

        panel.Controls.Add(_recordCard, 0, 0);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 1);
        panel.Controls.Add(actions, 0, 2);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 3);
        panel.Controls.Add(settings, 0, 4);
        return panel;
    }

    private RoundedPanel BuildSettingsCard()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 14, 16, 14),
            Radius = 16,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 10,
            Margin = new Padding(0)
        };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var headerTitle = new Label
        {
            Text = "回放设置",
            Font = Theme.LabelBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, 2)
        };
        var headerHint = new Label
        {
            Text = "F3 急停",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _stopHintLabel = headerHint;
        header.Controls.Add(headerTitle);
        header.Controls.Add(headerHint);
        header.Resize += (_, _) => headerHint.Location = new Point(header.Width - headerHint.Width, 3);

        _speedStepper = new NumberStepper
        {
            Min = 25,
            Max = 400,
            Step = 25,
            ValueScale = 100,
            Value = 100,
            Suffix = "×",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        _delaySeg = new SegmentedControl
        {
            Items = new[] { "关闭", "3 秒", "5 秒" },
            SelectedIndex = 1,
            AccentColor = Theme.Blue,
            SelectedTextColor = Theme.Text,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            Font = Theme.BodySmall
        };
        _loopStepper = new NumberStepper
        {
            Min = 1,
            Max = 999,
            Step = 1,
            Value = 1,
            Suffix = "次",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };

        _recordMovesCheck = new ModernCheckBox
        {
            Text = "录制鼠标移动轨迹",
            Checked = true,
            Dock = DockStyle.Fill,
            AccentColor = Theme.Green
        };
        _restoreCursorCheck = new ModernCheckBox
        {
            Text = "回放后恢复鼠标位置",
            Checked = true,
            Dock = DockStyle.Fill,
            AccentColor = Theme.Green
        };

        var speedRow = SettingRow("回放速度", _speedStepper);
        var delayRow = SettingRow("准备时间", _delaySeg);
        var loopRow = SettingRow("循环次数", _loopStepper);

        var tip = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 10,
            FillColor = Theme.Field,
            BorderColor = Theme.BorderSoft,
            Padding = new Padding(10, 7, 10, 7),
            Margin = new Padding(0, 4, 0, 0)
        };
        var tipText = new Label
        {
            Text = "回放按原始时间轴进行，速度按 0.25× 步进（0.25–4×）。\n停止时会自动松开所有按住的键。",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            AutoSize = false
        };
        tip.Controls.Add(tipText);

        table.Controls.Add(header, 0, 0);
        table.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 1);
        table.Controls.Add(speedRow, 0, 2);
        table.Controls.Add(delayRow, 0, 3);
        table.Controls.Add(loopRow, 0, 4);
        table.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 5);
        table.Controls.Add(_recordMovesCheck, 0, 6);
        table.Controls.Add(_restoreCursorCheck, 0, 7);
        table.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 8);
        table.Controls.Add(tip, 0, 9);

        card.Controls.Add(table);
        return card;
    }

    private TableLayoutPanel BuildLeftAuto()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 5,
            AutoScroll = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 268));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var positionCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 14, 16, 14),
            Radius = 16,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft
        };
        var positionTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 4
        };
        positionTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        positionTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        positionTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        positionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var positionHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var positionTitle = new Label
        {
            Text = "点击位置",
            Font = Theme.LabelBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, 2)
        };
        var positionHint = new Label
        {
            Text = "F5 取坐标",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _captureHintLabel = positionHint;
        positionHeader.Controls.Add(positionTitle);
        positionHeader.Controls.Add(positionHint);
        positionHeader.Resize += (_, _) => positionHint.Location = new Point(positionHeader.Width - positionHint.Width, 3);

        _autoPositionSeg = new SegmentedControl
        {
            Items = new[] { "当前鼠标位置", "固定坐标" },
            SelectedIndex = 0,
            AccentColor = Theme.Green,
            SelectedTextColor = Theme.Text,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            Font = Theme.BodySmall
        };

        var xyRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 2)
        };
        xyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        xyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        xyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        xyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var xLabel = new Label
        {
            Text = "X",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Font = Theme.BodySmall
        };
        var yLabel = new Label
        {
            Text = "Y",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Font = Theme.BodySmall
        };
        _autoX = new NumberStepper
        {
            Min = -10000,
            Max = 10000,
            Step = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 8, 2)
        };
        _autoY = new NumberStepper
        {
            Min = -10000,
            Max = 10000,
            Step = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 2)
        };
        xyRow.Controls.Add(xLabel, 0, 0);
        xyRow.Controls.Add(_autoX, 1, 0);
        xyRow.Controls.Add(yLabel, 2, 0);
        xyRow.Controls.Add(_autoY, 3, 0);

        _captureButton = new FlatButton
        {
            Text = "获取当前鼠标坐标",
            Icon = Glyph.Crosshair,
            Style = FlatButtonStyle.Ghost,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),
            Font = Theme.BodySmall
        };

        positionTable.Controls.Add(positionHeader, 0, 0);
        positionTable.Controls.Add(_autoPositionSeg, 0, 1);
        positionTable.Controls.Add(xyRow, 0, 2);
        positionTable.Controls.Add(_captureButton, 0, 3);
        positionCard.Controls.Add(positionTable);

        var settingsCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 14, 16, 14),
            Radius = 16,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft
        };
        var settingsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 5
        };
        settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var settingsHeader = new Label
        {
            Text = "连点参数",
            Font = Theme.LabelBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill
        };

        _autoButtonSeg = new SegmentedControl
        {
            Items = new[] { "左键", "右键", "中键" },
            SelectedIndex = 0,
            AccentColor = Theme.Green,
            SelectedTextColor = Theme.Text,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 5),
            Font = Theme.BodySmall
        };
        _autoInterval = new NumberStepper
        {
            Min = 10,
            Max = 60000,
            Step = 10,
            Value = 100,
            Suffix = "ms",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 5)
        };
        _autoCount = new NumberStepper
        {
            Min = 0,
            Max = 999999,
            Step = 1,
            Value = 0,
            Suffix = "次",
            ZeroText = "∞",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 5)
        };
        _autoJitter = new NumberStepper
        {
            Min = 0,
            Max = 50,
            Step = 1,
            Value = 0,
            Suffix = "px",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 5)
        };

        settingsTable.Controls.Add(settingsHeader, 0, 0);
        settingsTable.Controls.Add(SettingRow("鼠标键", _autoButtonSeg), 0, 1);
        settingsTable.Controls.Add(SettingRow("点击间隔", _autoInterval), 0, 2);
        settingsTable.Controls.Add(SettingRow("点击次数", _autoCount), 0, 3);
        settingsTable.Controls.Add(SettingRow("随机偏移", _autoJitter), 0, 4);
        settingsCard.Controls.Add(settingsTable);

        var tip = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 12,
            FillColor = Theme.Field,
            BorderColor = Theme.BorderSoft,
            Padding = new Padding(12, 8, 12, 8)
        };
        _autoTipLabel = new Label
        {
            Text = "F4 开始 / 停止连点  ·  F5 捕获当前坐标  ·  F3 急停\n次数设为 0 时持续点击，直到手动停止。",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            AutoSize = false
        };
        tip.Controls.Add(_autoTipLabel);

        panel.Controls.Add(positionCard, 0, 0);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 1);
        panel.Controls.Add(settingsCard, 0, 2);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 3);
        panel.Controls.Add(tip, 0, 4);
        return panel;
    }

    private TableLayoutPanel BuildRightMacro()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = new Label
        {
            Text = "录制历史",
            Font = Theme.Heading,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, 6)
        };
        _historyCount = new Label
        {
            Text = "0 条",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(90, 13)
        };
        _historySize = new Label
        {
            Text = "本地保存",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(145, 13)
        };

        _importButton = new FlatButton
        {
            Text = "导入",
            Icon = Glyph.Open,
            Style = FlatButtonStyle.Ghost,
            Size = new Size(76, 32),
            Font = Theme.BodySmall
        };
        _folderButton = new FlatButton
        {
            Text = "文件夹",
            Icon = Glyph.Open,
            Style = FlatButtonStyle.Ghost,
            Size = new Size(84, 32),
            Font = Theme.BodySmall
        };

        toolbar.Controls.Add(title);
        toolbar.Controls.Add(_historyCount);
        toolbar.Controls.Add(_historySize);
        toolbar.Controls.Add(_importButton);
        toolbar.Controls.Add(_folderButton);
        toolbar.Resize += (_, _) =>
        {
            _folderButton.Location = new Point(toolbar.Width - _folderButton.Width, 4);
            _importButton.Location = new Point(_folderButton.Left - _importButton.Width - 4, 4);
            _historySize.Left = Math.Min(_historySize.Left, _importButton.Left - _historySize.Width - 16);
        };

        var historyCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 16,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft,
            Padding = new Padding(8, 8, 4, 8)
        };

        _historyFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 8, 2),
            Margin = new Padding(0)
        };
        _historyEmpty = new Label
        {
            Dock = DockStyle.Fill,
            Text = "还没有录制记录\n\n按 F1 开始录制，停止后会自动保存在这里",
            Font = Theme.Body,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        historyCard.Controls.Add(_historyFlow);
        historyCard.Controls.Add(_historyEmpty);
        _historyEmpty.BringToFront();

        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 1);
        panel.Controls.Add(historyCard, 0, 2);
        return panel;
    }

    private RoundedPanel BuildRightAuto()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 16,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft,
            Padding = new Padding(24, 18, 24, 20)
        };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3
        };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = new Label
        {
            Text = "自动连点",
            Font = Theme.Hero,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, 4)
        };
        var subtitle = new Label
        {
            Text = "把重复点击交给轻录",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(142, 12)
        };
        _autoStatusLabel = new Label
        {
            Text = "就绪",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(_autoStatusLabel);
        header.Resize += (_, _) => _autoStatusLabel.Location =
            new Point(header.Width - _autoStatusLabel.Width, 15);

        _autoPulse = new AutoPulseView { Dock = DockStyle.Fill };

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        _autoStartButton = new FlatButton
        {
            Text = "开始连点",
            Icon = Glyph.Play,
            Hotkey = "F4",
            Style = FlatButtonStyle.Solid,
            AccentColor = Theme.Green,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 5, 0)
        };
        _autoStopButton = new FlatButton
        {
            Text = "停止",
            Icon = Glyph.Stop,
            Hotkey = "F3",
            Style = FlatButtonStyle.Ghost,
            Dock = DockStyle.Fill,
            Margin = new Padding(5, 0, 0, 0)
        };
        _autoFooterHint = new Label
        {
            Text = "连点开始前会短暂准备，让目标窗口获得焦点；F3 可随时停止。",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        bottom.Controls.Add(_autoStartButton, 0, 0);
        bottom.Controls.Add(_autoStopButton, 1, 0);
        bottom.Controls.Add(_autoFooterHint, 0, 1);
        bottom.SetColumnSpan(_autoFooterHint, 2);

        table.Controls.Add(header, 0, 0);
        table.Controls.Add(_autoPulse, 0, 1);
        table.Controls.Add(bottom, 0, 2);
        card.Controls.Add(table);
        return card;
    }

    private Panel BuildFooter()
    {
        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderSoft, 1);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var dot = new Label
        {
            Text = "●",
            Font = new Font("Microsoft YaHei UI", 7F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Theme.Green,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(0, 9)
        };
        var privacy = new Label
        {
            Text = "本地运行 · 不上传数据",
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(15, 7)
        };
        _footerHint = new Label
        {
            Text = "按 F1 开始录制",
            Font = Theme.BodySmall,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = false,
            Height = 18,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(210, 7)
        };
        _footerVersion = new Label
        {
            Text = "v1.1.6",
            Font = Theme.TinyMono,
            ForeColor = Theme.MutedDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        footer.Controls.Add(dot);
        footer.Controls.Add(privacy);
        footer.Controls.Add(_footerHint);
        footer.Controls.Add(_footerVersion);
        footer.Resize += (_, _) =>
        {
            _footerVersion.Location = new Point(footer.Width - _footerVersion.Width, 9);
            _footerHint.SetBounds(210, 7, Math.Max(50, footer.Width - 210 - _footerVersion.Width - 20), 18);
        };
        return footer;
    }

    private static TableLayoutPanel SettingRow(string label, Control control, int labelWidth = 92)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var text = new Label
        {
            Text = label,
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 4, 0, 4);
        row.Controls.Add(text, 0, 0);
        row.Controls.Add(control, 1, 0);
        return row;
    }
}
