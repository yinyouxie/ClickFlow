using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class AutoPulseView : Control
{
    private readonly Font _countFont = new("Microsoft YaHei UI", 38F, FontStyle.Bold, GraphicsUnit.Point);
    private long _count;
    private bool _running;
    private float _pulsePhase;
    private string _status = "准备就绪";
    private string _detail = "按 F4 开始连点，F3 随时停止";
    private string _position = "当前位置";

    public long Count
    {
        get => _count;
        set { _count = value; Invalidate(); }
    }

    public bool Running
    {
        get => _running;
        set { _running = value; Invalidate(); }
    }

    public float PulsePhase
    {
        get => _pulsePhase;
        set { _pulsePhase = value; Invalidate(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; Invalidate(); }
    }

    public string Detail
    {
        get => _detail;
        set { _detail = value; Invalidate(); }
    }

    public string PositionText
    {
        get => _position;
        set { _position = value; Invalidate(); }
    }

    public AutoPulseView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.Transparent;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _countFont.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var accent = _running ? Theme.Green : Theme.Blue;
        var centerX = Width / 2f;
        var centerY = Math.Max(150, Height * 0.39f);
        var baseRadius = Math.Min(118f, Math.Min(Width, Height) * 0.26f);

        for (var i = 2; i >= 0; i--)
        {
            var radius = baseRadius + 34 + i * 26;
            using var pen = new Pen(Theme.Alpha(accent, i == 0 ? 22 : 10), 1f);
            e.Graphics.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2, radius * 2);
        }

        if (_running)
        {
            var phase = Math.Clamp(_pulsePhase, 0f, 1f);
            var radius = baseRadius + phase * 40;
            using var pen = new Pen(Theme.Alpha(accent, (int)(70 * (1 - phase))), 1.6f);
            e.Graphics.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2, radius * 2);
        }

        var circle = new RectangleF(centerX - baseRadius, centerY - baseRadius, baseRadius * 2, baseRadius * 2);
        using (var brush = new SolidBrush(Theme.Alpha(accent, _running ? 24 : 16)))
        {
            e.Graphics.FillEllipse(brush, circle);
        }

        using (var pen = new Pen(Theme.Alpha(accent, _running ? 88 : 52), 1.3f))
        {
            e.Graphics.DrawEllipse(pen, circle);
        }

        var countText = _count.ToString("N0");
        TextRenderer.DrawText(
            e.Graphics,
            countText,
            _countFont,
            new Rectangle((int)circle.Left, (int)(circle.Top + baseRadius * 0.35f), (int)circle.Width, 66),
            Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            "次点击",
            Theme.Body,
            new Rectangle((int)circle.Left, (int)(circle.Top + baseRadius + 28), (int)circle.Width, 24),
            Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            _status,
            Theme.Heading,
            new Rectangle(20, (int)(centerY + baseRadius + 64), Width - 40, 30),
            _running ? Theme.Green : Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            _detail,
            Theme.BodySmall,
            new Rectangle(20, (int)(centerY + baseRadius + 95), Width - 40, 23),
            Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        var positionRect = new RectangleF(centerX - 150, centerY + baseRadius + 130, 300, 28);
        Theme.FillRounded(e.Graphics, positionRect, 8, Theme.Alpha(Theme.SurfaceHover, 105));
        Theme.DrawRounded(e.Graphics, positionRect, 8, Theme.BorderSoft);
        TextRenderer.DrawText(
            e.Graphics,
            _position,
            Theme.TinyMono,
            positionRect.ToRectangle(),
            Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }
}
