using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal class ModernCheckBox : Control
{
    private bool _checked;
    private bool _hovered;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Color AccentColor { get; set; } = Theme.Green;

    public event EventHandler? CheckedChanged;

    public ModernCheckBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        BackColor = Color.Transparent;
        Font = Theme.BodySmall;
        Height = 24;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.CheckButton;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var enabled = Enabled;
        var accent = enabled ? AccentColor : Theme.MutedDim;
        var box = new RectangleF(0, (Height - 16) / 2f, 16, 16);
        if (Checked)
        {
            Theme.FillRounded(e.Graphics, box, 5, accent);
            Theme.DrawRounded(e.Graphics, box, 5, Theme.Alpha(accent, 220));
            Theme.DrawGlyph(
                e.Graphics,
                Glyph.Check,
                new RectangleF(box.Left + 2, box.Top + 2, 12, 12),
                Theme.Bg,
                1.7f);
        }
        else
        {
            Theme.FillRounded(e.Graphics, box, 5, enabled ? Theme.Field : Theme.Blend(Theme.Field, Theme.Bg, 0.5));
            Theme.DrawRounded(
                e.Graphics,
                box,
                5,
                _hovered && enabled ? Theme.Alpha(AccentColor, 120) : Theme.Border);
        }

        var color = Enabled ? Theme.TextSoft : Theme.MutedDim;
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            new Rectangle(24, 0, Math.Max(0, Width - 24), Height + 1),
            color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData == Keys.Space || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            Checked = !Checked;
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}
