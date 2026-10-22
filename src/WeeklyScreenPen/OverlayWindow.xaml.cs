using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WeeklyScreenPen.Core;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace WeeklyScreenPen;

public partial class OverlayWindow : Window
{
    private readonly Action _bringToolbarTop;
    private IntPtr _hwnd;

    public DrawingCanvas Canvas { get; }

    public bool DrawMode { get; private set; }

    public event Action? DrawModeChanged;

    public OverlayWindow(bool drawMode, Action bringToolbarTop)
    {
        InitializeComponent();
        _bringToolbarTop = bringToolbarTop;
        DrawMode = drawMode;
        Canvas = new DrawingCanvas(Surface);

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        SourceInitialized += OnSourceInitialized;
        Activated += (_, _) => _bringToolbarTop();
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.RegisterHotKeys(_hwnd);
        HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
        ApplyMode();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        NativeMethods.UnregisterHotKeys(_hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            switch (wParam.ToInt32())
            {
                case 1:
                    ToggleDrawMode();
                    break;
                case 2:
                    Canvas.Undo();
                    break;
                case 3:
                    Canvas.Clear();
                    break;
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void ToggleDrawMode()
    {
        DrawMode = !DrawMode;
        ApplyMode();
        if (!DrawMode)
        {
            Canvas.End();
            Mouse.Capture(null);
        }
        DrawModeChanged?.Invoke();
    }

    private void ApplyMode()
    {
        if (_hwnd == IntPtr.Zero) return;
        NativeMethods.SetClickThrough(_hwnd, clickThrough: !DrawMode, noActivate: !DrawMode);
        Cursor = DrawMode ? Cursors.Cross : Cursors.Arrow;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!DrawMode) return;
        CaptureMouse();
        Canvas.Begin(e.GetPosition(Surface));
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!DrawMode || e.LeftButton != MouseButtonState.Pressed) return;
        Canvas.Move(e.GetPosition(Surface));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!DrawMode) return;
        Canvas.End();
        ReleaseMouseCapture();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Canvas.Clear();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Canvas.Undo();
            e.Handled = true;
        }
    }
}
