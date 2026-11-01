using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClickFlow.Core;

/// <summary>
/// Runs low-level mouse/keyboard hooks on a dedicated thread with its own message loop.
/// The callback only translates and forwards events, keeping hook latency low.
/// </summary>
internal sealed class GlobalHookService : IDisposable
{
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _mouseHook;
    private IntPtr _keyboardHook;
    private NativeMethods.LowLevelProc? _mouseProc;
    private NativeMethods.LowLevelProc? _keyboardProc;
    private volatile bool _enabled;
    private Exception? _startError;
    private bool _disposed;

    public event Action<RawInputEvent>? Input;

    public bool IsRunning => _thread is { IsAlive: true } && _enabled;

    public bool Start(out string error)
    {
        error = string.Empty;
        if (_disposed)
        {
            error = "录制器已经释放。";
            return false;
        }

        if (_thread is { IsAlive: true })
        {
            return true;
        }

        _ready.Reset();
        _startError = null;
        _enabled = false;
        _thread = new Thread(HookThread)
        {
            IsBackground = true,
            Name = "ClickFlow.InputHooks",
            Priority = ThreadPriority.AboveNormal
        };
        _thread.Start();

        if (!_ready.Wait(3000))
        {
            error = "启动全局输入监听超时。";
            Stop();
            return false;
        }

        if (_startError != null)
        {
            error = _startError.Message;
            Stop();
            return false;
        }

        _enabled = true;
        return true;
    }

    public void Stop()
    {
        _enabled = false;
        var thread = _thread;
        if (thread == null)
        {
            return;
        }

        if (_threadId != 0)
        {
            NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        try
        {
            if (!thread.Join(2000))
            {
                thread.Join(1000);
            }
        }
        catch
        {
            // Thread shutdown is best effort.
        }

        _thread = null;
        _threadId = 0;
        _mouseHook = IntPtr.Zero;
        _keyboardHook = IntPtr.Zero;
    }

    private void HookThread()
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        _mouseProc = MouseHookProc;
        _keyboardProc = KeyboardHookProc;

        try
        {
            var module = NativeMethods.GetModuleHandle(null);
            _mouseHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _mouseProc,
                module,
                0);

            if (_mouseHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安装鼠标监听钩子。");
            }

            _keyboardHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _keyboardProc,
                module,
                0);

            if (_keyboardHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安装键盘监听钩子。");
            }

            _ready.Set();

            while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }
        }
        catch (Exception ex)
        {
            _startError = ex;
            _ready.Set();
        }
        finally
        {
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            _ready.Set();
        }
    }

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _enabled)
        {
            try
            {
                var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if ((info.flags & NativeMethods.LLMHF_INJECTED) == 0)
                {
                    var message = wParam.ToInt32();
                    RawInputEvent? evt = message switch
                    {
                        NativeMethods.WM_MOUSEMOVE => new RawInputEvent(
                            RawInputKind.MouseMove, info.pt.X, info.pt.Y, 0, 0, false, false, 0),
                        NativeMethods.WM_LBUTTONDOWN => new RawInputEvent(
                            RawInputKind.MouseDown, info.pt.X, info.pt.Y, 0, 0, false, false, 0),
                        NativeMethods.WM_LBUTTONUP => new RawInputEvent(
                            RawInputKind.MouseUp, info.pt.X, info.pt.Y, 0, 0, false, false, 0),
                        NativeMethods.WM_RBUTTONDOWN => new RawInputEvent(
                            RawInputKind.MouseDown, info.pt.X, info.pt.Y, 1, 0, false, false, 0),
                        NativeMethods.WM_RBUTTONUP => new RawInputEvent(
                            RawInputKind.MouseUp, info.pt.X, info.pt.Y, 1, 0, false, false, 0),
                        NativeMethods.WM_MBUTTONDOWN => new RawInputEvent(
                            RawInputKind.MouseDown, info.pt.X, info.pt.Y, 2, 0, false, false, 0),
                        NativeMethods.WM_MBUTTONUP => new RawInputEvent(
                            RawInputKind.MouseUp, info.pt.X, info.pt.Y, 2, 0, false, false, 0),
                        NativeMethods.WM_XBUTTONDOWN => new RawInputEvent(
                            RawInputKind.MouseDown,
                            info.pt.X,
                            info.pt.Y,
                            ((info.mouseData >> 16) & 0xFFFF) == 1 ? 3 : 4,
                            0,
                            false,
                            false,
                            0),
                        NativeMethods.WM_XBUTTONUP => new RawInputEvent(
                            RawInputKind.MouseUp,
                            info.pt.X,
                            info.pt.Y,
                            ((info.mouseData >> 16) & 0xFFFF) == 1 ? 3 : 4,
                            0,
                            false,
                            false,
                            0),
                        NativeMethods.WM_MOUSEWHEEL => new RawInputEvent(
                            RawInputKind.Wheel,
                            info.pt.X,
                            info.pt.Y,
                            unchecked((short)((info.mouseData >> 16) & 0xFFFF)),
                            0,
                            false,
                            false,
                            0),
                        NativeMethods.WM_MOUSEHWHEEL => new RawInputEvent(
                            RawInputKind.Wheel,
                            info.pt.X,
                            info.pt.Y,
                            unchecked((short)((info.mouseData >> 16) & 0xFFFF)),
                            0,
                            false,
                            true,
                            0),
                        _ => null
                    };

                    if (evt.HasValue)
                    {
                        Input?.Invoke(evt.Value);
                    }
                }
            }
            catch
            {
                // A hook callback must never throw; Windows will drop the hook.
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _enabled)
        {
            try
            {
                var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                if ((info.flags & NativeMethods.LLKHF_INJECTED) == 0)
                {
                    var message = wParam.ToInt32();
                    if (message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
                    {
                        Input?.Invoke(new RawInputEvent(
                            RawInputKind.KeyDown, 0, 0, (int)info.vkCode, (int)info.scanCode,
                            (info.flags & NativeMethods.LLKHF_EXTENDED) != 0, false, (int)info.vkCode));
                    }
                    else if (message is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP)
                    {
                        Input?.Invoke(new RawInputEvent(
                            RawInputKind.KeyUp, 0, 0, (int)info.vkCode, (int)info.scanCode,
                            (info.flags & NativeMethods.LLKHF_EXTENDED) != 0, false, (int)info.vkCode));
                    }
                }
            }
            catch
            {
                // Keep the hook alive no matter what a subscriber does.
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _ready.Dispose();
    }
}
