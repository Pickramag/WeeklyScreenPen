using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WeeklyScreenPen.Core;

namespace WeeklyScreenPen;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notify;
    private readonly ToolStripMenuItem _showItem;
    private readonly ToolStripMenuItem _modeItem;
    private readonly ToolStripMenuItem _clearItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly OverlayWindow _overlay;
    private readonly ToolbarWindow _toolbar;
    private readonly DrawingCanvas _canvas;

    public event Action? ExitRequested;

    public TrayIcon(OverlayWindow overlay, ToolbarWindow toolbar, DrawingCanvas canvas)
    {
        _overlay = overlay;
        _toolbar = toolbar;
        _canvas = canvas;

        _showItem = new ToolStripMenuItem(Loc.T("tray.show"));
        _modeItem = new ToolStripMenuItem();
        _clearItem = new ToolStripMenuItem(Loc.T("tray.clear"));
        _exitItem = new ToolStripMenuItem(Loc.T("tray.exit"));

        _showItem.Click += (_, _) => _toolbar.ToggleVisible();
        _modeItem.Click += (_, _) => _overlay.ToggleDrawMode();
        _clearItem.Click += (_, _) => _canvas.Clear();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_showItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_modeItem);
        menu.Items.Add(_clearItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        _notify = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "WeeklyScreenPen",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notify.DoubleClick += (_, _) => _toolbar.ToggleVisible();

        _overlay.DrawModeChanged += OnDrawModeChanged;
        _toolbar.IsVisibleChanged += (_, _) =>
        {
            _showItem.Text = Loc.T(_toolbar.IsVisible ? "tray.hide" : "tray.show");
        };
        Loc.Changed += OnLanguageChanged;

        OnDrawModeChanged();
    }

    private static Icon LoadIcon()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("WeeklyScreenPen.Assets.icon.ico");
            if (stream != null) return new Icon(stream);
        }
        catch
        {
            // fall through
        }
        return SystemIcons.Application;
    }

    private void OnDrawModeChanged()
    {
        _modeItem.Text = Loc.T(_overlay.DrawMode ? "tray.draw" : "tray.cursor");
        _modeItem.Checked = _overlay.DrawMode;
        _showItem.Text = Loc.T(_toolbar.IsVisible ? "tray.hide" : "tray.show");
    }

    private void OnLanguageChanged()
    {
        _showItem.Text = Loc.T(_toolbar.IsVisible ? "tray.hide" : "tray.show");
        _clearItem.Text = Loc.T("tray.clear");
        _exitItem.Text = Loc.T("tray.exit");
        OnDrawModeChanged();
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }
}
