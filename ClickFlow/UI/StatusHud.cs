using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClickFlow.UI;

internal sealed class StatusHud : Form
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private string _status = string.Empty;
    private Color _accent = Theme.Green;
    private double? _progress;
    private float _pulse;
    private bool _showStopAction;

    public bool ShowStopAction
    {
        get => _showStopAction;
        set
        {
            if (_showStopAction == value)
            {
                return;
            }

            _showStopAction = value;
            Invalidate();
        }
    }

    public StatusHud()
    {
        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Theme.Bg;
        DoubleBuffered = true;
        Opacity = 0.97;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Size = new Size(390, 46);
        ResumeLayout(true);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            return cp;
        }
    }

    public void SetStatus(string text, Color accent, double? progress = null)
    {
        _status = text;
        _accent = accent;
        _progress = progress;
        if (!Visible)
        {
            var scale = WindowEffects.GetDpiScale(this);
            Size = new Size(
                (int)Math.Round(390 * scale),
                (int)Math.Round(46 * scale));
            var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(
                screen.Left + (screen.Width - Width) / 2,
                screen.Top + 12);
            Show();
        }

        TopMost = true;
        Invalidate();
    }

    public void SetPulse(float phase)
    {
        _pulse = Math.Clamp(phase, 0, 1);
        Invalidate();
    }

    public void HideHud()
    {
        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        Theme.FillRounded(e.Graphics, rect, 13, Theme.SurfaceRaised);
        Theme.DrawRounded(e.Graphics, rect, 13, Theme.Alpha(_accent, 75), 1.1f);

        var dotX = 20f;
        var dotY = Height / 2f - (_progress.HasValue ? 2f : 0f);
        var glow = 5f + _pulse * 5f;
        Theme.FillRounded(
            e.Graphics,
            new RectangleF(dotX - glow, dotY - glow, glow * 2, glow * 2),
            glow,
            Theme.Alpha(_accent, (int)(45 * (1 - _pulse))));
        using (var brush = new SolidBrush(_accent))
        {
            e.Graphics.FillEllipse(brush, dotX - 4, dotY - 4, 8, 8);
        }

        var rightPadding = _showStopAction ? 88 : 15;
        TextRenderer.DrawText(
            e.Graphics,
            _status,
            Theme.BodyBold,
            new Rectangle(35, 0, Math.Max(20, Width - 35 - rightPadding), Height - (_progress.HasValue ? 3 : 0)),
            Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if (_showStopAction)
        {
            var stop = new RectangleF(Width - 74, (Height - 26) / 2f, 60, 26);
            Theme.FillRounded(e.Graphics, stop, 8, Theme.Alpha(Theme.Accent, 24));
            Theme.DrawRounded(e.Graphics, stop, 8, Theme.Alpha(Theme.Accent, 100));
            TextRenderer.DrawText(
                e.Graphics,
                "停止",
                Theme.BodySmall,
                Rectangle.Round(stop),
                Theme.AccentSoft,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        if (_progress.HasValue)
        {
            var p = Math.Clamp(_progress.Value, 0, 1);
            var bar = new RectangleF(12, Height - 5.5f, Width - 24, 2.2f);
            Theme.FillRounded(e.Graphics, bar, 1.1f, Theme.Alpha(_accent, 35));
            if (p > 0)
            {
                Theme.FillRounded(
                    e.Graphics,
                    new RectangleF(bar.Left, bar.Top, Math.Max(2, bar.Width * (float)p), bar.Height),
                    1.1f,
                    _accent);
            }
        }
    }
}
