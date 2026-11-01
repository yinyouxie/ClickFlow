using System.Drawing.Drawing2D;

namespace ClickFlow.UI.Controls;

internal enum FlatButtonStyle
{
    Tint,
    Solid,
    Ghost
}

internal class FlatButton : Control
{
    private FlatButtonStyle _style = FlatButtonStyle.Tint;
    private Color _accentColor = Theme.Green;
    private Glyph _icon = Glyph.None;
    private string _hotkey = string.Empty;
    private bool _hovered;
    private bool _pressed;
    private float _radius = 12f;

    public FlatButtonStyle Style
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    public Glyph Icon
    {
        get => _icon;
        set { _icon = value; Invalidate(); }
    }

    public string Hotkey
    {
        get => _hotkey;
        set { _hotkey = value; Invalidate(); }
    }

    public float Radius
    {
        get => _radius;
        set { _radius = value; Invalidate(); }
    }

    public Color? CustomTextColor { get; set; }

    public FlatButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
        BackColor = Color.Transparent;
        Font = Theme.LabelBold;
        Cursor = Cursors.Hand;
        TabStop = true;
        Height = 44;
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0, 0, Width, Height);
        var enabled = Enabled;
        var accent = enabled ? AccentColor : Theme.MutedDim;

        var fill = Style switch
        {
            FlatButtonStyle.Solid => accent,
            FlatButtonStyle.Tint => Theme.Alpha(accent, _pressed ? 44 : _hovered ? 34 : 22),
            _ => _pressed
                ? Theme.Alpha(Theme.SurfaceHover, 230)
                : _hovered
                    ? Theme.Alpha(Theme.SurfaceHover, 150)
                    : Color.Transparent
        };

        if (fill.A > 0)
        {
            Theme.FillRounded(e.Graphics, rect, Radius, fill);
        }

        var border = Style switch
        {
            FlatButtonStyle.Solid => Color.Transparent,
            FlatButtonStyle.Tint => Theme.Alpha(accent, _hovered ? 90 : 52),
            _ => _hovered ? Theme.Border : Color.Transparent
        };

        if (border.A > 0)
        {
            Theme.DrawRounded(e.Graphics, rect, Radius, border);
        }

        if (Focused && ShowFocusCues)
        {
            Theme.DrawRounded(
                e.Graphics,
                RectangleF.Inflate(rect, -3, -3),
                Math.Max(2, Radius - 2),
                Theme.Alpha(accent, 140),
                1.2f);
        }

        var textColor = CustomTextColor ??
            (Style == FlatButtonStyle.Solid ? Theme.Bg : enabled ? Theme.Text : Theme.MutedDim);
        var iconColor = Style == FlatButtonStyle.Solid ? Theme.Bg : accent;
        var offset = _pressed ? 1 : 0;

        var hasText = !string.IsNullOrEmpty(Text);
        var scale = WindowEffects.GetDpiScale(this);
        var hotkeyWidth = 0;
        if (!string.IsNullOrEmpty(Hotkey))
        {
            var hkSize = TextRenderer.MeasureText(Hotkey, Theme.TinyMono);
            hotkeyWidth = hkSize.Width + (int)Math.Round(12 * scale);
        }

        if (IconOnly)
        {
            var size = Math.Min(22, Math.Min(Width, Height) - 18);
            var iconRect = new RectangleF(
                (Width - size) / 2f,
                (Height - size) / 2f + offset,
                size,
                size);
            Theme.DrawGlyph(e.Graphics, Icon, iconRect, iconColor, 1.7f);
        }
        else
        {
            var pad = Width < 100 ? 10f : 14f;
            var iconSize = 17f;
            var x = pad;
            if (Icon != Glyph.None)
            {
                var iconRect = new RectangleF(x, (Height - iconSize) / 2f + offset, iconSize, iconSize);
                Theme.DrawGlyph(e.Graphics, Icon, iconRect, iconColor, 1.7f);
                x += iconSize + 8f;
            }

            var textRight = Width - pad - (hotkeyWidth > 0 ? hotkeyWidth + 8 : 0);
            var centered = Icon == Glyph.None && hotkeyWidth == 0;
            var textRect = centered
                ? new Rectangle(6, 0, Math.Max(0, Width - 12), Height + 1)
                : new Rectangle(
                    (int)x,
                    0,
                    Math.Max(0, (int)(textRight - x)),
                    Height + 1);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textRect,
                textColor,
                (centered ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left) |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);

            if (hotkeyWidth > 0)
            {
                var hkHeight = 20 * scale;
                var hkRect = new RectangleF(
                    Width - pad - hotkeyWidth,
                    (Height - hkHeight) / 2f,
                    hotkeyWidth,
                    hkHeight);
                Theme.FillRounded(
                    e.Graphics,
                    hkRect,
                    6 * scale,
                    Style == FlatButtonStyle.Solid
                        ? Theme.Alpha(Theme.Bg, 32)
                        : Theme.Alpha(Theme.Bg, 120));
                Theme.DrawRounded(
                    e.Graphics,
                    hkRect,
                    6 * scale,
                    Style == FlatButtonStyle.Solid
                        ? Theme.Alpha(Theme.Bg, 45)
                        : Theme.Border);
                TextRenderer.DrawText(
                    e.Graphics,
                    Hotkey,
                    Theme.TinyMono,
                    hkRect.ToRectangle(),
                    Style == FlatButtonStyle.Solid ? Theme.Bg : Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }
        }
    }

    private bool IconOnly => string.IsNullOrEmpty(Text) && Icon != Glyph.None;

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Focus();
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var shouldClick = e.Button == MouseButtons.Left &&
                          ClientRectangle.Contains(e.Location) &&
                          Enabled;
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
        if (shouldClick)
        {
            OnClick(EventArgs.Empty);
        }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
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
        return keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            e.Handled = true;
            OnClick(EventArgs.Empty);
        }

        base.OnKeyDown(e);
    }
}

internal static class RectangleExtensions
{
    public static Rectangle ToRectangle(this RectangleF rect) =>
        Rectangle.Round(rect);
}
