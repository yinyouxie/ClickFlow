using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal sealed class HelpDialog : Form
{
    public HelpDialog()
    {
        SuspendLayout();
        Text = "使用帮助 · 轻录";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(900, 860);
        Padding = new Padding(26, 18, 26, 20);

        var title = new Label
        {
            Text = "使用帮助",
            Font = Theme.Hero,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 38
        };
        var subtitle = new Label
        {
            Text = "三段式操作：录制 → 调整 → 回放。所有内容只保存在本机。",
            Font = Theme.Body,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 26
        };

        var hotkeyCard = BuildHotkeyCard();
        hotkeyCard.Dock = DockStyle.Top;
        hotkeyCard.Height = 260;

        var tips = BuildSection(
            L.T("录制小贴士", "Recording tips"),
            L.T(
                "• 倒计时结束后，轻录会自动最小化，把前台让给目标窗口。\n" +
                "• 拖动鼠标会被记录成一条“拖动/滑动”操作，轨迹和耗时可原样回放。\n" +
                "• 滚轮、右键、中键、键盘组合键都可以录制。\n" +
                "• 回放速度支持 0.25× ~ 4×，循环次数最多 999 次。\n" +
                "• 每次停止录制都会自动进入录制历史。",
                "• After the countdown, ClickFlow minimizes and returns focus to the target window.\n" +
                "• Dragging is recorded as a drag/swipe with its path and duration.\n" +
                "• Wheel, right/middle click and keyboard shortcuts are supported.\n" +
                "• Playback speed is 0.25×–4×; loops can go up to 999.\n" +
                "• Every finished recording is added to History."));
        tips.Dock = DockStyle.Top;
        tips.Height = 176;

        var limits = BuildSection(
            L.T("使用边界", "Limits"),
            L.T(
                "轻录使用 Windows 标准输入注入，无法操作以管理员权限运行的窗口、UAC 安全桌面、锁屏界面，\n" +
                "也无法绕过部分游戏或安全软件的反作弊限制。需要操作管理员窗口时，请以管理员身份启动轻录。\n" +
                "如果 F1–F5 被其它软件占用，轻录会自动改用 Ctrl+F1–F5；回放时按 F3/Esc 或点击顶部急停。\n" +
                "每次录制都会自动进入录制历史；删除记录会同时删除本地文件。",
                "ClickFlow uses standard Windows input injection. It cannot operate elevated windows, the UAC secure desktop or the lock screen, and some games/security tools block simulated input. Run ClickFlow as administrator when needed.\n" +
                "If F1–F5 are occupied, ClickFlow automatically uses Ctrl+F1–F5. Press F3/Esc or click Stop above during playback.\n" +
                "Every recording is saved to History; deleting it also deletes the local file."));
        limits.Dock = DockStyle.Fill;

        var close = new FlatButton
        {
            Text = "知道了",
            Style = FlatButtonStyle.Tint,
            AccentColor = Theme.Green,
            Size = new Size(108, 40),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(ClientSize.Width - 134, ClientSize.Height - 58)
        };
        close.Click += (_, _) => Close();

        var spacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
        var spacer2 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

        Controls.Add(limits);
        Controls.Add(spacer2);
        Controls.Add(tips);
        Controls.Add(spacer);
        Controls.Add(hotkeyCard);
        Controls.Add(subtitle);
        Controls.Add(title);
        Controls.Add(close);
        close.BringToFront();
        ResumeLayout(true);
        L.Apply(this);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowEffects.ApplyDarkChrome(this);
    }



    private static RoundedPanel BuildHotkeyCard()
    {
        var card = new RoundedPanel
        {
            Radius = 14,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft,
            Padding = new Padding(16, 12, 16, 12)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 7
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28f));
        }

        var heading = new Label
        {
            Text = "全局快捷键",
            Font = Theme.LabelBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill
        };
        table.Controls.Add(heading, 0, 0);
        table.SetColumnSpan(heading, 2);

        AddHotkeyRow(table, 1, "F1", "开始 / 停止录制");
        AddHotkeyRow(table, 2, "F2", "开始 / 停止回放");
        AddHotkeyRow(table, 3, "F3", "紧急停止（录制、回放、连点全部停止）");
        AddHotkeyRow(table, 4, "F4", "开始 / 停止自动连点");
        AddHotkeyRow(table, 5, "F5", "捕获当前鼠标坐标");
        AddHotkeyRow(table, 6, "Ctrl+H", "打开使用帮助");

        card.Controls.Add(table);
        return card;
    }

    private static void AddHotkeyRow(TableLayoutPanel table, int row, string key, string description)
    {
        var chip = new Label
        {
            Text = key,
            Font = Theme.Mono,
            ForeColor = Theme.AccentSoft,
            BackColor = Theme.SurfaceRaised,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 12, 4)
        };
        var text = new Label
        {
            Text = description,
            Font = Theme.Body,
            ForeColor = Theme.TextSoft,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        table.Controls.Add(chip, 0, row);
        table.Controls.Add(text, 1, row);
    }

    private static RoundedPanel BuildSection(string heading, string content)
    {
        var card = new RoundedPanel
        {
            Radius = 14,
            FillColor = Theme.Surface,
            BorderColor = Theme.BorderSoft,
            Padding = new Padding(16, 12, 16, 12)
        };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 2
        };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = heading,
            Font = Theme.LabelBold,
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill
        };
        var body = new Label
        {
            Text = content,
            Font = Theme.BodySmall,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill
        };
        table.Controls.Add(title, 0, 0);
        table.Controls.Add(body, 0, 1);
        card.Controls.Add(table);
        return card;
    }
}
