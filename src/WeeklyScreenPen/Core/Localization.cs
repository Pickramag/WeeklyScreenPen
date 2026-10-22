namespace WeeklyScreenPen.Core;

public enum Language
{
    Ru,
    En
}

public static class Loc
{
    public static Language Current { get; private set; } = Language.Ru;

    public static event Action? Changed;

    private static readonly Dictionary<string, (string Ru, string En)> Map = new()
    {
        ["mode.draw"] = ("Рисовать", "Draw"),
        ["mode.cursor"] = ("Курсор", "Cursor"),
        ["tip.mode.draw"] = ("Режим рисования — рисуйте по экрану (Ctrl+Alt+D)", "Draw mode — draw over the screen (Ctrl+Alt+D)"),
        ["tip.mode.cursor"] = ("Режим курсора — клики проходят в другие окна (Ctrl+Alt+D)", "Cursor mode — clicks pass through to other windows (Ctrl+Alt+D)"),
        ["tool.pen"] = ("Ручка", "Pen"),
        ["tool.marker"] = ("Маркер", "Marker"),
        ["tool.highlighter"] = ("Хайлайтер", "Highlighter"),
        ["tool.eraser"] = ("Ластик", "Eraser"),
        ["size"] = ("Размер", "Size"),
        ["color.custom"] = ("Свой цвет…", "Custom color…"),
        ["undo.tip"] = ("Отменить (Ctrl+Alt+Z)", "Undo (Ctrl+Alt+Z)"),
        ["clear.tip"] = ("Очистить экран (Ctrl+Alt+X)", "Clear screen (Ctrl+Alt+X)"),
        ["close.tip"] = ("Свернуть в трей", "Minimize to tray"),
        ["lang.toggle"] = ("Переключить язык / Switch language", "Switch language / Переключить язык"),
        ["footer.hotkeys"] = ("Ctrl+Alt+D — режим · Ctrl+Alt+Z — отмена · Ctrl+Alt+X — очистить · Esc — очистить", "Ctrl+Alt+D — mode · Ctrl+Alt+Z — undo · Ctrl+Alt+X — clear · Esc — clear"),
        ["tray.show"] = ("Показать панель", "Show toolbar"),
        ["tray.hide"] = ("Скрыть панель", "Hide toolbar"),
        ["tray.draw"] = ("Режим рисования", "Draw mode"),
        ["tray.cursor"] = ("Режим курсора", "Cursor mode"),
        ["tray.clear"] = ("Очистить экран", "Clear screen"),
        ["tray.exit"] = ("Выход", "Exit"),
        ["app.name"] = ("WeeklyScreenPen", "WeeklyScreenPen"),
    };

    public static string T(string key) =>
        Map.TryGetValue(key, out var v) ? (Current == Language.Ru ? v.Ru : v.En) : key;

    public static void Set(Language lang)
    {
        if (Current == lang) return;
        Current = lang;
        Changed?.Invoke();
    }

    public static void Toggle() => Set(Current == Language.Ru ? Language.En : Language.Ru);
}
