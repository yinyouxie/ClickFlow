using System.Drawing.Drawing2D;
using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal enum Glyph
{
    None,
    Record,
    Play,
    Stop,
    Save,
    Open,
    Clear,
    Plus,
    Minus,
    Mouse,
    Keyboard,
    Drag,
    Scroll,
    Help,
    Crosshair,
    Check,
    Close,
    ChevronUp,
    ChevronDown,
    Palette,
    Settings
}

internal sealed record ThemePalette(
    string Id,
    string Name,
    Color Bg,
    Color Surface,
    Color SurfaceRaised,
    Color SurfaceHover,
    Color Field,
    Color Border,
    Color BorderSoft);

internal static class Theme
{
    public static Color Bg { get; private set; } = Hex("#0A0D12");
    public static Color Surface { get; private set; } = Hex("#12161D");
    public static Color SurfaceRaised { get; private set; } = Hex("#171C25");
    public static Color SurfaceHover { get; private set; } = Hex("#1D2430");
    public static Color Field { get; private set; } = Hex("#0E1218");
    public static Color Border { get; private set; } = Hex("#242C39");
    public static Color BorderSoft { get; private set; } = Hex("#1B222D");

    public static IReadOnlyList<ThemePalette> Presets { get; } = new[]
    {
        new ThemePalette("deep-space", "深空黑", Hex("#0A0D12"), Hex("#12161D"), Hex("#171C25"), Hex("#1D2430"), Hex("#0E1218"), Hex("#242C39"), Hex("#1B222D")),
        new ThemePalette("graphite", "石墨灰", Hex("#15171C"), Hex("#1C1F25"), Hex("#242830"), Hex("#2C313B"), Hex("#111318"), Hex("#343A45"), Hex("#252A33")),
        new ThemePalette("midnight", "午夜蓝", Hex("#0A1220"), Hex("#111C2E"), Hex("#18253A"), Hex("#1F2F49"), Hex("#080F1A"), Hex("#263A57"), Hex("#18283D")),
        new ThemePalette("forest", "墨绿", Hex("#08130F"), Hex("#0F1D17"), Hex("#162821"), Hex("#1D342A"), Hex("#06100C"), Hex("#244536"), Hex("#172C23")),
        new ThemePalette("wine", "酒红", Hex("#160C11"), Hex("#201219"), Hex("#2A1821"), Hex("#35202B"), Hex("#10080C"), Hex("#432A36"), Hex("#2C1B24")),
        new ThemePalette("amber", "暖褐", Hex("#141009"), Hex("#1E1810"), Hex("#292116"), Hex("#352A1C"), Hex("#0F0C07"), Hex("#453824"), Hex("#2B2216")),
        new ThemePalette("violet", "紫夜", Hex("#100D18"), Hex("#181324"), Hex("#211A30"), Hex("#2A223D"), Hex("#0B0912"), Hex("#382C4D"), Hex("#241C33")),
        new ThemePalette("ocean", "深海", Hex("#07151B"), Hex("#0D2028"), Hex("#142A34"), Hex("#1B3643"), Hex("#050F13"), Hex("#254251"), Hex("#142A33"))
    };

    public static ThemePalette CurrentPalette { get; private set; } = Resolve("deep-space", 0);

    public static readonly Color Text = Hex("#F2F5FA");
    public static readonly Color TextSoft = Hex("#C5CDDB");
    public static readonly Color Muted = Hex("#8B96A9");
    public static readonly Color MutedDim = Hex("#626D7E");
    public static readonly Color Accent = Hex("#FF6B6B");
    public static readonly Color AccentSoft = Hex("#FF9A8A");
    public static readonly Color Green = Hex("#46D9A0");
    public static readonly Color Blue = Hex("#6FA8FF");
    public static readonly Color Amber = Hex("#FFC857");
    public static readonly Color Purple = Hex("#B59BFF");
    public static readonly Color Cyan = Hex("#58D6E8");

    public static Font Body { get; private set; } = new("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font BodySmall { get; private set; } = new("Microsoft YaHei UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font BodyBold { get; private set; } = new("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font Label { get; private set; } = new("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font LabelBold { get; private set; } = new("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font Heading { get; private set; } = new("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font Hero { get; private set; } = new("Microsoft YaHei UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font Display { get; private set; } = new("Microsoft YaHei UI", 30F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font TinyMono { get; private set; } = new("Consolas", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font Mono { get; private set; } = new("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);

    public static void ApplyFontScale(float scale)
    {
        scale = Math.Clamp(scale, 0.9f, 1.1f);
        Body = MakeFont("Microsoft YaHei UI", 9F * scale, FontStyle.Regular);
        BodySmall = MakeFont("Microsoft YaHei UI", 8.25F * scale, FontStyle.Regular);
        BodyBold = MakeFont("Microsoft YaHei UI", 9F * scale, FontStyle.Bold);
        Label = MakeFont("Microsoft YaHei UI", 10F * scale, FontStyle.Regular);
        LabelBold = MakeFont("Microsoft YaHei UI", 10F * scale, FontStyle.Bold);
        Heading = MakeFont("Microsoft YaHei UI", 12F * scale, FontStyle.Bold);
        Hero = MakeFont("Microsoft YaHei UI", 18F * scale, FontStyle.Bold);
        Display = MakeFont("Microsoft YaHei UI", 30F * scale, FontStyle.Bold);
        TinyMono = MakeFont("Consolas", 8.25F * scale, FontStyle.Regular);
        Mono = MakeFont("Consolas", 9F * scale, FontStyle.Regular);
    }

    private static Font MakeFont(string family, float size, FontStyle style)
    {
        try
        {
            return new Font(family, size, style, GraphicsUnit.Point);
        }
        catch
        {
            var fallbackFamily = SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif;
            return new Font(fallbackFamily, size, style, GraphicsUnit.Point);
        }
    }

    public static Color Hex(string hex) => ColorTranslator.FromHtml(hex);

    public static Color Alpha(Color color, int alpha) =>
        Color.FromArgb(Math.Clamp(alpha, 0, 255), color);

    public static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * amount),
            (int)(a.R + (b.R - a.R) * amount),
            (int)(a.G + (b.G - a.G) * amount),
            (int)(a.B + (b.B - a.B) * amount));
    }

    public static ThemePalette Resolve(string? id, int customArgb)
    {
        if (string.Equals(id, "custom", StringComparison.OrdinalIgnoreCase) &&
            customArgb != 0)
        {
            return FromBackground("自定义", Color.FromArgb(customArgb));
        }

        return Presets.FirstOrDefault(p =>
                   string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
               ?? Presets[0];
    }

    public static ThemePalette FromBackground(string name, Color background)
    {
        var bg = background;
        var guard = 0;
        while (Luminance(bg) > 0.42 && guard++ < 12)
        {
            bg = Blend(bg, Color.Black, 0.18);
        }

        var surface = Blend(bg, Color.White, 0.055);
        var raised = Blend(bg, Color.White, 0.095);
        var hover = Blend(bg, Color.White, 0.145);
        var field = Blend(bg, Color.Black, 0.34);
        var border = Blend(bg, Color.White, 0.16);
        var borderSoft = Blend(bg, Color.White, 0.09);
        return new ThemePalette(
            "custom",
            name,
            bg,
            surface,
            raised,
            hover,
            field,
            border,
            borderSoft);
    }

    public static void Apply(ThemePalette palette)
    {
        Bg = palette.Bg;
        Surface = palette.Surface;
        SurfaceRaised = palette.SurfaceRaised;
        SurfaceHover = palette.SurfaceHover;
        Field = palette.Field;
        Border = palette.Border;
        BorderSoft = palette.BorderSoft;
        CurrentPalette = palette;
    }

    public static void RecolorControlTree(
        Control root,
        ThemePalette oldPalette,
        ThemePalette newPalette)
    {
        if (root is RoundedPanel card)
        {
            card.FillColor = MapColor(card.FillColor, oldPalette, newPalette);
            card.BorderColor = MapColor(card.BorderColor, oldPalette, newPalette);
        }

        if (root.BackColor.A != 0 && root.BackColor != Color.Transparent)
        {
            root.BackColor = MapColor(root.BackColor, oldPalette, newPalette);
        }

        root.Invalidate();
        foreach (Control child in root.Controls)
        {
            RecolorControlTree(child, oldPalette, newPalette);
        }
    }

    private static Color MapColor(Color color, ThemePalette oldPalette, ThemePalette newPalette)
    {
        if (color == oldPalette.Bg) return newPalette.Bg;
        if (color == oldPalette.Surface) return newPalette.Surface;
        if (color == oldPalette.SurfaceRaised) return newPalette.SurfaceRaised;
        if (color == oldPalette.SurfaceHover) return newPalette.SurfaceHover;
        if (color == oldPalette.Field) return newPalette.Field;
        if (color == oldPalette.Border) return newPalette.Border;
        if (color == oldPalette.BorderSoft) return newPalette.BorderSoft;
        return color;
    }

    private static double Luminance(Color color) =>
        (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;

    public static GraphicsPath Rounded(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        radius = Math.Max(0, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
        if (radius < 0.5f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var d = radius * 2f;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics g, RectangleF bounds, float radius, Color color)
    {
        using var path = Rounded(bounds, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    public static void FillRounded(Graphics g, RectangleF bounds, float radius, Brush brush)
    {
        using var path = Rounded(bounds, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRounded(
        Graphics g,
        RectangleF bounds,
        float radius,
        Color color,
        float width = 1f)
    {
        using var path = Rounded(RectangleF.Inflate(bounds, -width / 2f, -width / 2f), radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    public static string FormatDuration(long ms)
    {
        if (ms < 0)
        {
            ms = 0;
        }

        var t = TimeSpan.FromMilliseconds(ms);
        if (t.TotalHours >= 1)
        {
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        return $"{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}";
    }

    public static string FormatClock(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    public static void DrawGlyph(Graphics g, Glyph glyph, RectangleF r, Color color, float stroke = 1.8f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, stroke)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(color);

        var cx = r.Left + r.Width / 2f;
        var cy = r.Top + r.Height / 2f;
        var s = Math.Min(r.Width, r.Height);

        switch (glyph)
        {
            case Glyph.Record:
            {
                var d = s * 0.62f;
                g.FillEllipse(brush, cx - d / 2f, cy - d / 2f, d, d);
                break;
            }
            case Glyph.Play:
            {
                var path = new GraphicsPath();
                var d = s * 0.72f;
                path.AddPolygon(new[]
                {
                    new PointF(cx - d * 0.30f, cy - d * 0.52f),
                    new PointF(cx + d * 0.55f, cy),
                    new PointF(cx - d * 0.30f, cy + d * 0.52f)
                });
                g.FillPath(brush, path);
                path.Dispose();
                break;
            }
            case Glyph.Stop:
            {
                var d = s * 0.50f;
                FillRounded(g, new RectangleF(cx - d / 2f, cy - d / 2f, d, d), s * 0.12f, color);
                break;
            }
            case Glyph.Save:
            {
                var w = s * 0.68f;
                var h = s * 0.68f;
                var rect = new RectangleF(cx - w / 2f, cy - h / 2f, w, h);
                DrawRounded(g, rect, 2.5f, color, stroke);
                g.FillRectangle(brush, rect.Left + w * 0.18f, rect.Top, w * 0.45f, h * 0.30f);
                g.DrawRectangle(pen, rect.Left + w * 0.20f, rect.Top + h * 0.55f, w * 0.58f, h * 0.31f);
                break;
            }
            case Glyph.Open:
            {
                var w = s * 0.72f;
                var h = s * 0.54f;
                var rect = new RectangleF(cx - w / 2f, cy - h / 2f + s * 0.06f, w, h);
                DrawRounded(g, rect, 2.8f, color, stroke);
                g.DrawLine(pen, rect.Left + w * 0.08f, rect.Top + h * 0.30f,
                    rect.Left + w * 0.48f, rect.Top + h * 0.30f);
                g.DrawLine(pen, rect.Left + w * 0.48f, rect.Top + h * 0.30f,
                    rect.Left + w * 0.60f, rect.Top + h * 0.05f);
                break;
            }
            case Glyph.Clear:
            {
                var w = s * 0.50f;
                var h = s * 0.62f;
                var rect = new RectangleF(cx - w / 2f, cy - h / 2f + s * 0.08f, w, h);
                DrawRounded(g, rect, 2.5f, color, stroke);
                g.DrawLine(pen, rect.Left - s * 0.10f, rect.Top - s * 0.04f,
                    rect.Right + s * 0.10f, rect.Top - s * 0.04f);
                g.DrawLine(pen, cx - s * 0.12f, rect.Top - s * 0.12f,
                    cx + s * 0.12f, rect.Top - s * 0.12f);
                g.DrawLine(pen, cx - s * 0.08f, rect.Top + h * 0.22f,
                    cx - s * 0.08f, rect.Bottom - h * 0.16f);
                g.DrawLine(pen, cx + s * 0.08f, rect.Top + h * 0.22f,
                    cx + s * 0.08f, rect.Bottom - h * 0.16f);
                break;
            }
            case Glyph.Plus:
                g.DrawLine(pen, cx - s * 0.30f, cy, cx + s * 0.30f, cy);
                g.DrawLine(pen, cx, cy - s * 0.30f, cx, cy + s * 0.30f);
                break;
            case Glyph.Minus:
                g.DrawLine(pen, cx - s * 0.30f, cy, cx + s * 0.30f, cy);
                break;
            case Glyph.Mouse:
            {
                var w = s * 0.48f;
                var h = s * 0.72f;
                var rect = new RectangleF(cx - w / 2f, cy - h / 2f, w, h);
                DrawRounded(g, rect, w / 2f, color, stroke);
                g.DrawLine(pen, cx, rect.Top + h * 0.10f, cx, rect.Top + h * 0.38f);
                break;
            }
            case Glyph.Keyboard:
            {
                var w = s * 0.78f;
                var h = s * 0.56f;
                var rect = new RectangleF(cx - w / 2f, cy - h / 2f, w, h);
                DrawRounded(g, rect, 3f, color, stroke);
                for (var i = 0; i < 3; i++)
                {
                    var x = rect.Left + w * (0.22f + i * 0.22f);
                    g.DrawLine(pen, x, rect.Top + h * 0.35f, x, rect.Top + h * 0.35f + 0.1f);
                }
                g.DrawLine(pen, rect.Left + w * 0.22f, rect.Bottom - h * 0.23f,
                    rect.Right - w * 0.22f, rect.Bottom - h * 0.23f);
                break;
            }
            case Glyph.Drag:
            {
                g.DrawLine(pen, r.Left + s * 0.20f, r.Bottom - s * 0.24f,
                    r.Right - s * 0.24f, r.Top + s * 0.22f);
                g.DrawLine(pen, r.Right - s * 0.24f, r.Top + s * 0.22f,
                    r.Right - s * 0.44f, r.Top + s * 0.22f);
                g.DrawLine(pen, r.Right - s * 0.24f, r.Top + s * 0.22f,
                    r.Right - s * 0.24f, r.Top + s * 0.42f);
                g.FillEllipse(brush, r.Left + s * 0.12f, r.Bottom - s * 0.32f, s * 0.16f, s * 0.16f);
                break;
            }
            case Glyph.Scroll:
            {
                var w = s * 0.44f;
                var h = s * 0.68f;
                var rect = new RectangleF(cx - w / 2f, cy - h / 2f, w, h);
                DrawRounded(g, rect, w / 2f, color, stroke);
                g.DrawLine(pen, cx - s * 0.07f, cy - s * 0.05f, cx, cy - s * 0.14f);
                g.DrawLine(pen, cx, cy - s * 0.14f, cx + s * 0.07f, cy - s * 0.05f);
                g.DrawLine(pen, cx - s * 0.07f, cy + s * 0.10f, cx, cy + s * 0.19f);
                g.DrawLine(pen, cx, cy + s * 0.19f, cx + s * 0.07f, cy + s * 0.10f);
                break;
            }
            case Glyph.Crosshair:
            {
                var d = s * 0.52f;
                g.DrawEllipse(pen, cx - d / 2f, cy - d / 2f, d, d);
                g.DrawLine(pen, cx - s * 0.40f, cy, cx - d / 2f - 1.5f, cy);
                g.DrawLine(pen, cx + d / 2f + 1.5f, cy, cx + s * 0.40f, cy);
                g.DrawLine(pen, cx, cy - s * 0.40f, cx, cy - d / 2f - 1.5f);
                g.DrawLine(pen, cx, cy + d / 2f + 1.5f, cx, cy + s * 0.40f);
                g.FillEllipse(brush, cx - 1.6f, cy - 1.6f, 3.2f, 3.2f);
                break;
            }
            case Glyph.Help:
            {
                var d = s * 0.62f;
                g.DrawEllipse(pen, cx - d / 2f, cy - d / 2f, d, d);
                using var font = new Font("Microsoft YaHei UI", Math.Max(7f, s * 0.32f), FontStyle.Bold, GraphicsUnit.Pixel);
                using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("?", font, brush, new RectangleF(cx - d / 2f, cy - d / 2f, d, d), fmt);
                break;
            }
            case Glyph.Check:
                g.DrawLine(pen, r.Left + s * 0.22f, cy, r.Left + s * 0.44f, cy + s * 0.20f);
                g.DrawLine(pen, r.Left + s * 0.44f, cy + s * 0.20f, r.Right - s * 0.18f, cy - s * 0.22f);
                break;
            case Glyph.Close:
                g.DrawLine(pen, cx - s * 0.20f, cy - s * 0.20f, cx + s * 0.20f, cy + s * 0.20f);
                g.DrawLine(pen, cx + s * 0.20f, cy - s * 0.20f, cx - s * 0.20f, cy + s * 0.20f);
                break;
            case Glyph.ChevronDown:
                g.DrawLine(pen, cx - s * 0.22f, cy - s * 0.08f, cx, cy + s * 0.14f);
                g.DrawLine(pen, cx, cy + s * 0.14f, cx + s * 0.22f, cy - s * 0.08f);
                break;
            case Glyph.ChevronUp:
                g.DrawLine(pen, cx - s * 0.22f, cy + s * 0.08f, cx, cy - s * 0.14f);
                g.DrawLine(pen, cx, cy - s * 0.14f, cx + s * 0.22f, cy + s * 0.08f);
                break;
            case Glyph.Palette:
            {
                var d = s * 0.62f;
                g.DrawEllipse(pen, cx - d / 2f, cy - d / 2f, d, d);
                using var dot = new SolidBrush(color);
                g.FillEllipse(dot, cx - d * 0.24f, cy - d * 0.18f, d * 0.16f, d * 0.16f);
                g.FillEllipse(dot, cx + d * 0.06f, cy - d * 0.28f, d * 0.14f, d * 0.14f);
                g.FillEllipse(dot, cx + d * 0.10f, cy + d * 0.08f, d * 0.15f, d * 0.15f);
                break;
            }
            case Glyph.Settings:
            {
                var outer = s * 0.26f;
                var inner = s * 0.08f;
                g.DrawEllipse(pen, cx - outer, cy - outer, outer * 2, outer * 2);
                g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2, inner * 2);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var r1 = outer + s * 0.02f;
                    var r2 = s * 0.40f;
                    g.DrawLine(
                        pen,
                        cx + (float)Math.Cos(angle) * r1,
                        cy + (float)Math.Sin(angle) * r1,
                        cx + (float)Math.Cos(angle) * r2,
                        cy + (float)Math.Sin(angle) * r2);
                }

                break;
            }
        }
    }
}
