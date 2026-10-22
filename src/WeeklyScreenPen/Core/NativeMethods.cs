using System.Runtime.InteropServices;

namespace WeeklyScreenPen.Core;

internal static class NativeMethods
{
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WM_HOTKEY = 0x0312;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static void SetClickThrough(IntPtr hwnd, bool clickThrough, bool noActivate)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        style &= ~(WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
        if (clickThrough) style |= WS_EX_TRANSPARENT;
        if (noActivate) style |= WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, style);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }

    public static void BringTopmost(IntPtr hwnd) =>
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    public static bool RegisterHotKeys(IntPtr hwnd) =>
        RegisterHotKey(hwnd, 1, MOD_CONTROL | MOD_ALT, (uint)'D') &&
        RegisterHotKey(hwnd, 2, MOD_CONTROL | MOD_ALT, (uint)'Z') &&
        RegisterHotKey(hwnd, 3, MOD_CONTROL | MOD_ALT, (uint)'X');

    public static void UnregisterHotKeys(IntPtr hwnd)
    {
        for (int id = 1; id <= 3; id++) UnregisterHotKey(hwnd, id);
    }
}
