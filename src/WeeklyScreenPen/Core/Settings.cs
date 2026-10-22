using System.IO;
using System.Text.Json;

namespace WeeklyScreenPen.Core;

public sealed class AppSettings
{
    public string ColorHex { get; set; } = "#E5484D";
    public double Size { get; set; } = 4;
    public string Tool { get; set; } = nameof(Core.Tool.Pen);
    public string Language { get; set; } = "ru";
    public bool DrawMode { get; set; } = true;
    public double? ToolbarLeft { get; set; }
    public double? ToolbarTop { get; set; }

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (s != null) return s;
            }
        }
        catch
        {
            // corrupted settings are not fatal
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // read-only folder is not fatal
        }
    }
}
