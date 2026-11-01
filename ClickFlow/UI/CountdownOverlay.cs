using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ClickFlow.UI;

internal sealed class CountdownOverlay : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = new();
    private readonly int _totalMs;
    private readonly string _caption;
    private readonly Color _accent;
    private readonly Font _numberFont = new("Microsoft YaHei UI", 58F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _completed;

    private CountdownOverlay(int seconds, string caption, Color accent)
    {
        _totalMs = seconds * 1000;
        _caption = caption;
        _accent = accent;

        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Theme.SurfaceRaised;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Size = new Size(240, 240);
        Resize += (_, _) => UpdateRegion();

        _timer = new System.Windows.Forms.Timer { Interval = 30 };
        _timer.Tick += (_, _) =>
        {
            if (_clock.ElapsedMilliseconds >= _totalMs)
            {
                Complete(true);
            }
            else
            {
                Invalidate();
            }
        };
        ResumeLayout(true);
    }

    public static Task<bool> RunAsync(
        IWin32Window owner,
        int seconds,
        string caption,
        Color accent,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(false);
        }

        if (seconds <= 0)
        {
            return Task.FromResult(true);
        }

        var form = new CountdownOverlay(seconds, caption, accent);
        form.Show(owner);
        form.BringToFront();
        form.Activate();
        form._clock.Start();
        form._timer.Start();

        if (cancellationToken.CanBeCanceled)
        {
            form._cancellationRegistration = cancellationToken.Register(form.CancelFromToken);
        }

        return form._completion.Task;
    }

    private void CancelFromToken()
    {
        if (_completed || IsDisposed)
        {
            return;
        }

        if (IsHandleCreated)
        {
            try
            {
                BeginInvoke(new Action(() => Complete(false)));
            }
            catch
            {
                Complete(false);
            }
        }
        else
        {
            Complete(false);
        }
    }

    protected override bool ShowWithoutActivation => false;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var scale = WindowEffects.GetDpiScale(this);
        Size = new Size(
            (int)Math.Round(240 * scale),
            (int)Math.Round(240 * scale));
        CenterOnCursorScreen();
        UpdateRegion();
    }

    private void CenterOnCursorScreen()
    {
        var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            screen.Left + (screen.Width - Width) / 2,
            screen.Top + (screen.Height - Height) / 2);
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = new GraphicsPath();
        path.AddEllipse(0, 0, Width, Height);
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(1, 1, Width - 2, Height - 2);
        using var fill = new SolidBrush(Theme.SurfaceRaised);
        e.Graphics.FillEllipse(fill, rect);
        using var border = new Pen(Theme.Alpha(_accent, 90), 1.4f);
        e.Graphics.DrawEllipse(border, rect);

        var progress = 1f - Math.Clamp(_clock.ElapsedMilliseconds / (float)_totalMs, 0f, 1f);
        using var track = new Pen(Theme.Alpha(_accent, 22), 6f);
        e.Graphics.DrawArc(track, 13, 13, Width - 26, Height - 26, -90, 359.5f);
        using var arc = new Pen(_accent, 6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.DrawArc(arc, 13, 13, Width - 26, Height - 26, -90, 360f * progress);

        var secondsLeft = Math.Max(1, (int)Math.Ceiling((_totalMs - _clock.ElapsedMilliseconds) / 1000.0));
        TextRenderer.DrawText(
            e.Graphics,
            secondsLeft.ToString(),
            _numberFont,
            new Rectangle(0, 52, Width, 92),
            Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            _caption,
            Theme.Label,
            new Rectangle(0, 142, Width, 26),
            Theme.TextSoft,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            "按 Esc 取消",
            Theme.BodySmall,
            new Rectangle(0, 170, Width, 22),
            Theme.MutedDim,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Complete(false);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        _cancellationRegistration.Dispose();
        if (!_completed)
        {
            _completed = true;
            _completion.TrySetResult(false);
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationRegistration.Dispose();
            _timer.Dispose();
            _numberFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Complete(bool result)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _timer.Stop();
        _completion.TrySetResult(result);
        Close();
    }
}
