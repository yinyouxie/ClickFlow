using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClickFlow.Core;

/// <summary>
/// Blocks physical keyboard input and mouse buttons while a macro is playing,
/// while allowing the player's injected events. Mouse movement stays available
/// so the on-screen emergency stop can be reached. F3 always stops playback and
/// remains blocked from the target application.
/// </summary>
internal sealed class InputGuard : IDisposable
{
    private const int VkF3 = 0x72;
    private const int VkEscape = 0x1B;

    private readonly object _areaLock = new();
    private readonly List<Rectangle> _stopAreas = new();
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
    private Func<bool>? _blockMovementWhen;

    public event Action? StopRequested;

    public bool IsRunning => _thread is { IsAlive: true } && _enabled;

    public bool Start(
        IEnumerable<Rectangle> stopAreas,
        Func<bool>? blockMovementWhen,
        out string error)
    {
        error = string.Empty;
        if (_disposed)
        {
            error = "输入锁定器已经释放。";
            return false;
        }

        _blockMovementWhen = blockMovementWhen;
        if (_thread is { IsAlive: true })
        {
            UpdateStopAreas(stopAreas);
            return true;
        }

        UpdateStopAreas(stopAreas);
        _ready.Reset();
        _startError = null;
        _enabled = false;
        _thread = new Thread(HookThread)
        {
            IsBackground = true,
            Name = "ClickFlow.PlaybackInputGuard",
            Priority = ThreadPriority.AboveNormal
        };
        _thread.Start();

        if (!_ready.Wait(3000))
        {
            error = "启动输入锁定超时。";
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

    public void UpdateStopAreas(IEnumerable<Rectangle> areas)
    {
        lock (_areaLock)
        {
            _stopAreas.Clear();
            _stopAreas.AddRange(areas);
        }
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
            // Best effort during shutdown.
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
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安装回放鼠标锁定钩子。");
            }

            _keyboardHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _keyboardProc,
                module,
                0);
            if (_keyboardHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安装回放键盘锁定钩子。");
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
                    if (message is NativeMethods.WM_LBUTTONDOWN
                        or NativeMethods.WM_RBUTTONDOWN
                        or NativeMethods.WM_MBUTTONDOWN
                        or NativeMethods.WM_XBUTTONDOWN)
                    {
                        if (message == NativeMethods.WM_LBUTTONDOWN &&
                            IsInStopArea(new Point(info.pt.X, info.pt.Y)))
                        {
                            StopRequested?.Invoke();
                        }

                        return 1;
                    }

                    if (message is NativeMethods.WM_LBUTTONUP
                        or NativeMethods.WM_RBUTTONUP
                        or NativeMethods.WM_MBUTTONUP
                        or NativeMethods.WM_XBUTTONUP)
                    {
                        return 1;
                    }

                    if (message is NativeMethods.WM_MOUSEWHEEL or NativeMethods.WM_MOUSEHWHEEL)
                    {
                        return 1;
                    }

                    if (message == NativeMethods.WM_MOUSEMOVE &&
                        _blockMovementWhen?.Invoke() == true)
                    {
                        // During a recorded drag, physical movement must not
                        // alter the pointer path.
                        return 1;
                    }

                    // Outside drags, movement is allowed so the user can reach
                    // the emergency stop area.
                    return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                }
            }
            catch
            {
                // Never leave the system with a dead input hook.
                return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
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
                    if (info.vkCode is VkF3 or VkEscape)
                    {
                        if (message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
                        {
                            StopRequested?.Invoke();
                        }

                        return 1;
                    }

                    if (message is NativeMethods.WM_KEYDOWN
                        or NativeMethods.WM_KEYUP
                        or NativeMethods.WM_SYSKEYDOWN
                        or NativeMethods.WM_SYSKEYUP)
                    {
                        return 1;
                    }
                }
            }
            catch
            {
                return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private bool IsInStopArea(Point point)
    {
        lock (_areaLock)
        {
            foreach (var area in _stopAreas)
            {
                if (!area.IsEmpty && area.Contains(point))
                {
                    return true;
                }
            }
        }

        return false;
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
