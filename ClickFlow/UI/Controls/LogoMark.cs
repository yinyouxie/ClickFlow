using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class LogoMark : Control
{
    public LogoMark()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.Transparent;
        Size = new Size(42, 42);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        using var gradient = new LinearGradientBrush(
            rect,
            Theme.Accent,
            Theme.Purple,
            135f);
        Theme.FillRounded(e.Graphics, rect, 12, gradient);

        using var shine = new LinearGradientBrush(
            new RectangleF(0, 0, Width, Height * 0.55f),
            Color.FromArgb(58, Color.White),
            Color.FromArgb(0, Color.White),
            90f);
        using (var shinePath = Theme.Rounded(new RectangleF(0, 0, Width, Height * 0.58f), 12))
        {
            e.Graphics.FillPath(shine, shinePath);
        }

        var cx = Width / 2f;
        var cy = Height / 2f;
        var cursor = new GraphicsPath();
        var s = Math.Min(Width, Height);
        cursor.AddPolygon(new[]
        {
            new PointF(cx - s * 0.20f, cy - s * 0.23f),
            new PointF(cx + s * 0.24f, cy + s * 0.04f),
            new PointF(cx + s * 0.04f, cy + s * 0.09f),
            new PointF(cx + s * 0.13f, cy + s * 0.29f),
            new PointF(cx + s * 0.02f, cy + s * 0.34f),
            new PointF(cx - s * 0.06f, cy + s * 0.14f),
            new PointF(cx - s * 0.21f, cy + s * 0.27f)
        });
        cursor.CloseFigure();
        using (var fill = new SolidBrush(Color.White))
        {
            e.Graphics.FillPath(fill, cursor);
        }

        cursor.Dispose();

        using var ring = new Pen(Color.FromArgb(115, Color.White), 1.2f);
        e.Graphics.DrawArc(ring, cx - s * 0.31f, cy - s * 0.31f, s * 0.62f, s * 0.62f, -55, 95);
    }
}
