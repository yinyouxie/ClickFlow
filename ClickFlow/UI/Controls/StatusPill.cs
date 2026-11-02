using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class StatusPill : Control
{
    private Color _dotColor = Theme.Green;
    private bool _pulse;
    private float _pulsePhase;
    private bool _autoWidth = true;

    public Color DotColor
    {
        get => _dotColor;
        set
        {
            if (_dotColor == value)
            {
                return;
            }

            _dotColor = value;
            Invalidate();
        }
    }

    public bool Pulse
    {
        get => _pulse;
        set
        {
            if (_pulse == value)
            {
                return;
            }

            _pulse = value;
            Invalidate();
        }
    }

    public float PulsePhase
    {
        get => _pulsePhase;
        set
        {
            if (Math.Abs(_pulsePhase - value) < 0.001f)
            {
                return;
            }

            _pulsePhase = value;
            Invalidate();
        }
    }

    public bool AutoWidth
    {
        get => _autoWidth;
        set => _autoWidth = value;
    }

    public StatusPill()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.Transparent;
        Font = Theme.BodySmall;
        Height = 30;
        Width = 150;
        Text = L.T("待机", "Idle");
    }

    [AllowNull]
    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value ?? string.Empty;
            if (AutoWidth)
            {
                var measured = TextRenderer.MeasureText(base.Text, Font);
                Width = Math.Max(70, measured.Width + 38);
            }

            Invalidate();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        Theme.FillRounded(e.Graphics, rect, Height / 2f, Theme.SurfaceRaised);
        Theme.DrawRounded(e.Graphics, rect, Height / 2f, Theme.BorderSoft);

        var dotX = 15f;
        var dotY = Height / 2f;
        if (Pulse)
        {
            var phase = Math.Clamp(_pulsePhase, 0f, 1f);
            var glow = 3.5f + phase * 5f;
            Theme.FillRounded(
                e.Graphics,
                new RectangleF(dotX - glow, dotY - glow, glow * 2, glow * 2),
                glow,
                Theme.Alpha(DotColor, (int)(55 * (1 - phase))));
        }

        using (var brush = new SolidBrush(DotColor))
        {
            e.Graphics.FillEllipse(brush, dotX - 3.5f, dotY - 3.5f, 7, 7);
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            new Rectangle(26, 0, Math.Max(0, Width - 34), Height + 1),
            Theme.TextSoft,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }
}
