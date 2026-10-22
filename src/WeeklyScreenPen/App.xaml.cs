using System.Windows;
using WeeklyScreenPen.Core;
using Application = System.Windows.Application;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace WeeklyScreenPen;

public partial class App : Application
{
    private Mutex? _instanceMutex;
    private AppSettings _settings = new();
    private OverlayWindow? _overlay;
    private ToolbarWindow? _toolbar;
    private TrayIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(true, "WeeklyScreenPen_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();
        Loc.Set(_settings.Language == "en" ? Language.En : Language.Ru);

        ToolbarWindow? toolbar = null;
        _overlay = new OverlayWindow(_settings.DrawMode, () => toolbar?.BringToTop());
        _toolbar = new ToolbarWindow(_overlay.Canvas, _overlay, _settings);
        toolbar = _toolbar;

        _overlay.Show();
        _toolbar.Show();

        _tray = new TrayIcon(_overlay, _toolbar, _overlay.Canvas);
        _tray.ExitRequested += () =>
        {
            _toolbar?.AllowClose();
            Shutdown();
        };

        _overlay.DrawModeChanged += SaveSettings;
        _overlay.Canvas.Changed += SaveSettings;
        _toolbar.SettingsChanged += SaveSettings;
        Loc.Changed += SaveSettings;
    }

    private void SaveSettings()
    {
        if (_overlay != null) _settings.DrawMode = _overlay.DrawMode;
        _toolbar?.SaveTo(_settings);
        _settings.Language = Loc.Current == Language.En ? "en" : "ru";
        _settings.Save();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveSettings();
        _tray?.Dispose();

        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
