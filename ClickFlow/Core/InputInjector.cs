using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ClickFlow.Core;

internal static class InputInjector
{
    public static Point GetCursorPosition()
    {
        if (NativeMethods.GetCursorPos(out var p))
        {
            return new Point(p.X, p.Y);
        }

        return Point.Empty;
    }

    public static void MoveTo(int x, int y)
    {
        var vx = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var vy = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var vw = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN));
        var vh = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));

        var nx = (int)Math.Round((x - vx) * 65535.0 / Math.Max(1, vw - 1));
        var ny = (int)Math.Round((y - vy) * 65535.0 / Math.Max(1, vh - 1));

        SendMouse(nx, ny, 0,
            NativeMethods.MOUSEEVENTF_MOVE |
            NativeMethods.MOUSEEVENTF_ABSOLUTE |
            NativeMethods.MOUSEEVENTF_VIRTUALDESK);
    }

    public static void MouseButton(MouseButtonKind button, bool down)
    {
        var (flag, data) = button switch
        {
            MouseButtonKind.Left => (down
                ? NativeMethods.MOUSEEVENTF_LEFTDOWN
                : NativeMethods.MOUSEEVENTF_LEFTUP, 0u),
            MouseButtonKind.Right => (down
                ? NativeMethods.MOUSEEVENTF_RIGHTDOWN
                : NativeMethods.MOUSEEVENTF_RIGHTUP, 0u),
            MouseButtonKind.Middle => (down
                ? NativeMethods.MOUSEEVENTF_MIDDLEDOWN
                : NativeMethods.MOUSEEVENTF_MIDDLEUP, 0u),
            MouseButtonKind.X1 => (down
                ? NativeMethods.MOUSEEVENTF_XDOWN
                : NativeMethods.MOUSEEVENTF_XUP, 1u),
            MouseButtonKind.X2 => (down
                ? NativeMethods.MOUSEEVENTF_XDOWN
                : NativeMethods.MOUSEEVENTF_XUP, 2u),
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        SendMouse(0, 0, data, flag);
    }

    public static void Wheel(int delta, bool horizontal)
    {
        SendMouse(0, 0, unchecked((uint)delta),
            horizontal ? NativeMethods.MOUSEEVENTF_HWHEEL : NativeMethods.MOUSEEVENTF_WHEEL);
    }

    public static void Key(int virtualKey, int scanCode, bool extended, bool down)
    {
        var useScan = scanCode > 0;
        var flags = useScan ? NativeMethods.KEYEVENTF_SCANCODE : 0u;
        if (extended)
        {
            flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
        }

        if (!down)
        {
            flags |= NativeMethods.KEYEVENTF_KEYUP;
        }

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = useScan ? (ushort)0 : (ushort)virtualKey,
                    wScan = useScan ? (ushort)scanCode : (ushort)0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        Send(inputs: new[] { input });
    }

    private static void SendMouse(int dx, int dy, uint data, uint flags)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            u = new NativeMethods.INPUTUNION
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = data,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        Send(inputs: new[] { input });
    }

    private static void Send(NativeMethods.INPUT[] inputs)
    {
        var size = Marshal.SizeOf<NativeMethods.INPUT>();
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
        if (sent != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            throw new Win32Exception(
                error,
                "系统拒绝了模拟输入。请确认没有处于锁屏、UAC 安全桌面或更高权限窗口的前台。");
        }
    }
}
