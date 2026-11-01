using System.Runtime.InteropServices;

namespace ClickFlow.UI;

internal static class WindowEffects
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int WmNclButtonDown = 0x00A1;
    private const int HtCaption = 0x0002;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public static void ApplyDarkChrome(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        try
        {
            var dark = 1;
            DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));

            var rounded = 2;
            DwmSetWindowAttribute(form.Handle, DwmwaWindowCornerPreference, ref rounded, sizeof(int));

            var border = ColorRef(Theme.Border);
            DwmSetWindowAttribute(form.Handle, DwmwaBorderColor, ref border, sizeof(int));

            var caption = ColorRef(Theme.Bg);
            DwmSetWindowAttribute(form.Handle, DwmwaCaptionColor, ref caption, sizeof(int));
        }
        catch
        {
            // Older Windows builds simply keep the default chrome.
        }
    }

    public static void BeginDrag(Form form)
    {
        ReleaseCapture();
        SendMessage(form.Handle, WmNclButtonDown, HtCaption, IntPtr.Zero);
    }

    public static float GetDpiScale(Control control)
    {
        try
        {
            if (control.IsHandleCreated)
            {
                return Math.Max(1f, GetDpiForWindow(control.Handle) / 96f);
            }
        }
        catch
        {
            // Fall back to the managed value below.
        }

        return Math.Max(1f, control.DeviceDpi / 96f);
    }

    private static int ColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);
}
