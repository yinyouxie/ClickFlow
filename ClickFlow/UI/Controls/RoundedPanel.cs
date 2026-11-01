using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class RoundedPanel : Panel
{
    private float _radius = 16f;
    private Color _fillColor = Theme.Surface;
    private Color _borderColor = Theme.Border;
    private float _borderWidth = 1f;
    private bool _showBorder = true;

    public float Radius
    {
        get => _radius;
        set { _radius = value; Invalidate(); }
    }

    public Color FillColor
    {
        get => _fillColor;
        set { _fillColor = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    public float BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = value; Invalidate(); }
    }

    public bool ShowBorder
    {
        get => _showBorder;
        set { _showBorder = value; Invalidate(); }
    }

    public RoundedPanel()
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

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        using var path = Theme.Rounded(rect, Radius);
        using var brush = new SolidBrush(FillColor);
        e.Graphics.FillPath(brush, path);

        if (ShowBorder && BorderWidth > 0)
        {
            Theme.DrawRounded(
                e.Graphics,
                new RectangleF(0, 0, Width, Height),
                Radius,
                BorderColor,
                BorderWidth);
        }

        base.OnPaint(e);
    }
}
