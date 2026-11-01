using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class StatTile : Control
{
    private string _title = string.Empty;
    private string _value = "0";
    private Color _valueColor = Theme.Text;

    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    public Color ValueColor
    {
        get => _valueColor;
        set { _valueColor = value; Invalidate(); }
    }

    public StatTile()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.Transparent;
        Height = 58;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        Theme.FillRounded(e.Graphics, rect, 12, Theme.Surface);
        Theme.DrawRounded(e.Graphics, rect, 12, Theme.BorderSoft);

        TextRenderer.DrawText(
            e.Graphics,
            Title,
            Theme.BodySmall,
            new Rectangle(13, 8, Width - 26, 18),
            Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            Value,
            Theme.Heading,
            new Rectangle(13, 25, Width - 26, 27),
            ValueColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }
}
