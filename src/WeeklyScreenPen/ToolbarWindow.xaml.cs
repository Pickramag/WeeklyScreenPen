using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using WeeklyScreenPen.Core;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Geometry = System.Windows.Media.Geometry;
using WpfColorDialog = System.Windows.Forms.ColorDialog;
using WinForms = System.Windows.Forms;

namespace WeeklyScreenPen;

public partial class ToolbarWindow : Window
{
    private readonly DrawingCanvas _canvas;
    private readonly OverlayWindow _overlay;
    private readonly AppSettings _settings;

    private readonly ToggleButton[] _toolButtons;
    private ToggleButton[] _swatchButtons = Array.Empty<ToggleButton>();
    private bool _ready;
    private bool _allowClose;

    public event Action? SettingsChanged;

    private static readonly Geometry CursorGeometry =
        Geometry.Parse("M3,1.5 L3,14.2 L6.3,11.1 L8.4,15.7 L10.5,14.8 L8.4,10.4 L12.8,10.4 Z");

    private static readonly Geometry PenGeometry =
        Geometry.Parse("M13.2,2.8 L6.2,9.8 M6.6,8.4 L8.2,10 L4.4,13.8 L2.4,14.4 L3,12.4 Z");

    public ToolbarWindow(DrawingCanvas canvas, OverlayWindow overlay, AppSettings settings)
    {
        InitializeComponent();
        _canvas = canvas;
        _overlay = overlay;
        _settings = settings;

        _toolButtons = new[] { PenButton, MarkerButton, HighlighterButton, EraserButton };
        _swatchButtons = SwatchHost.Children.OfType<ToggleButton>().ToArray();

        if (Enum.TryParse<Tool>(settings.Tool, out var restoredTool)) _canvas.Tool = restoredTool;
        if (ColorConverter.ConvertFromString(settings.ColorHex) is Color restoredColor)
            _canvas.Color = restoredColor;

        SizeSlider.Value = Math.Clamp(settings.Size, 1, 48);
        _canvas.Size = SizeSlider.Value;
        SelectTool(_canvas.Tool);
        SyncSwatches();
        UpdateSizePreview();
        UpdateUndoState();

        if (settings.ToolbarLeft is { } lx && settings.ToolbarTop is { } ly && IsOnScreen(lx, ly))
        {
            Left = lx;
            Top = ly;
        }
        else
        {
            Left = Math.Max(0, (SystemParameters.PrimaryScreenWidth - 640) / 2);
            Top = 24;
        }

        _overlay.DrawModeChanged += OnDrawModeChanged;
        Loc.Changed += OnLanguageChanged;
        _canvas.Changed += OnCanvasChanged;
        MouseLeftButtonDown += OnRootMouseDown;

        UpdateModeButton();
        ApplyLocalization();
        _ready = true;
    }

    public void BringToTop()
    {
        if (!IsVisible) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero) NativeMethods.BringTopmost(hwnd);
    }

    public void ToggleVisible()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            BringToTop();
            Activate();
        }
    }

    public void AllowClose() => _allowClose = true;

    public void SaveTo(AppSettings settings)
    {
        settings.Size = SizeSlider.Value;
        settings.Tool = _canvas.Tool.ToString();
        settings.ColorHex = "#" + _canvas.Color.ToString().Substring(3);
        if (IsVisible)
        {
            settings.ToolbarLeft = Left;
            settings.ToolbarTop = Top;
        }
    }

    private void OnRootMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            DragMove();
        }
        catch
        {
            // drag interrupted
        }
        if (_ready) SettingsChanged?.Invoke();
    }

    // ---------- mode ----------

    private void Mode_Click(object sender, RoutedEventArgs e) => _overlay.ToggleDrawMode();

    private void OnDrawModeChanged() => UpdateModeButton();

    private void UpdateModeButton()
    {
        ModeButton.IsChecked = _overlay.DrawMode;
        ModeIcon.Data = _overlay.DrawMode ? CursorGeometry : PenGeometry;
        ModeButton.ToolTip = Loc.T(_overlay.DrawMode ? "tip.mode.cursor" : "tip.mode.draw");
    }

    // ---------- tools ----------

    private void Tool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton btn || btn.Tag is not string tag) return;
        if (Enum.TryParse<Tool>(tag, out var tool)) SelectTool(tool);
    }

    private void SelectTool(Tool tool)
    {
        _canvas.Tool = tool;
        foreach (var b in _toolButtons)
            b.IsChecked = string.Equals(b.Tag as string, tool.ToString(), StringComparison.Ordinal);
        UpdateSizePreview();
        if (_ready) SettingsChanged?.Invoke();
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => _canvas.Undo();

    private void Clear_Click(object sender, RoutedEventArgs e) => _canvas.Clear();

    private void OnCanvasChanged()
    {
        UpdateUndoState();
        UpdateSizePreview();
    }

    private void UpdateUndoState() => UndoButton.IsEnabled = _canvas.Count > 0;

    // ---------- color ----------

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton btn || btn.Tag is not string hex) return;
        if (ColorConverter.ConvertFromString(hex) is not Color color) return;

        _canvas.Color = color;
        foreach (var s in _swatchButtons)
            s.IsChecked = ReferenceEquals(s, btn);
        UpdateSizePreview();
        if (_ready) SettingsChanged?.Invoke();
    }

    private void Custom_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WpfColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(_canvas.Color.R, _canvas.Color.G, _canvas.Color.B)
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK) return;

        _canvas.Color = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
        foreach (var s in _swatchButtons) s.IsChecked = false;
        UpdateSizePreview();
        if (_ready) SettingsChanged?.Invoke();
    }

    private void SyncSwatches()
    {
        string current = "#" + _canvas.Color.ToString().Substring(3);
        foreach (var s in _swatchButtons)
            s.IsChecked = string.Equals(s.Tag as string, current, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- size ----------

    private void Size_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized || _canvas == null) return;
        _canvas.Size = SizeSlider.Value;
        UpdateSizePreview();
    }

    private void Size_Commit(object sender, RoutedEventArgs e)
    {
        if (_ready) SettingsChanged?.Invoke();
    }

    private void UpdateSizePreview()
    {
        if (SizePreview == null || _canvas == null) return;
        double w = Math.Clamp(_canvas.PreviewWidth, 4, 28);
        SizePreview.Width = w;
        SizePreview.Height = w;
        SizePreview.Fill = new SolidColorBrush(_canvas.Color);
        SizePreview.Opacity = _canvas.Tool == Tool.Eraser ? 0.65 : 1.0;
        SizePreview.ToolTip = $"{_canvas.PreviewWidth:0} px";
    }

    // ---------- lang / close ----------

    private void Lang_Click(object sender, RoutedEventArgs e) => Loc.Toggle();

    private void OnLanguageChanged() => ApplyLocalization();

    private void ApplyLocalization()
    {
        LangText.Text = Loc.Current == WeeklyScreenPen.Core.Language.Ru ? "RU" : "EN";
        SizeLabel.Text = Loc.T("size");
        FooterText.Text = Loc.T("footer.hotkeys");
        PenButton.ToolTip = Loc.T("tool.pen");
        MarkerButton.ToolTip = Loc.T("tool.marker");
        HighlighterButton.ToolTip = Loc.T("tool.highlighter");
        EraserButton.ToolTip = Loc.T("tool.eraser");
        UndoButton.ToolTip = Loc.T("undo.tip");
        ClearButton.ToolTip = Loc.T("clear.tip");
        CustomColorButton.ToolTip = Loc.T("color.custom");
        CloseButton.ToolTip = Loc.T("close.tip");
        LangButton.ToolTip = Loc.T("lang.toggle");
        UpdateModeButton();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
    }

    private static bool IsOnScreen(double x, double y) =>
        x >= SystemParameters.VirtualScreenLeft &&
        y >= SystemParameters.VirtualScreenTop &&
        x < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80 &&
        y < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 80;
}
