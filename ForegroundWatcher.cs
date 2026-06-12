namespace BackgroundMute;

internal sealed class ForegroundWatcher : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly IntPtr _hookHandle;

    public event Action<int>? ForegroundChanged;

    public ForegroundWatcher()
    {
        _callback = HandleWinEvent;
        _hookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _callback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
    }

    public int GetCurrentForegroundProcessId()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return 0;
        }

        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return (int)processId;
    }

    private void HandleWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        ForegroundChanged?.Invoke((int)processId);
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            _ = NativeMethods.UnhookWinEvent(_hookHandle);
        }
    }
}
