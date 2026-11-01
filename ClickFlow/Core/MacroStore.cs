using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickFlow.Core;

public sealed class AppSettings
{
    public double PlaybackSpeed { get; set; } = 1.0;
    public int LoopCount { get; set; } = 1;
    public int StartDelaySeconds { get; set; } = 3;
    public bool RecordMouseMoves { get; set; } = true;
    public bool RestoreCursor { get; set; } = true;
    public string ThemeId { get; set; } = "deep-space";
    public int CustomThemeArgb { get; set; }
    public double UiScale { get; set; } = 1.0;
    public string Language { get; set; } = "zh-CN";
    public string StartupPage { get; set; } = "macro";
    public bool CheckUpdatesOnStartup { get; set; }

    public bool AutoUseCurrentPosition { get; set; } = true;
    public int AutoX { get; set; }
    public int AutoY { get; set; }
    public MouseButtonKind AutoButton { get; set; } = MouseButtonKind.Left;
    public int AutoIntervalMs { get; set; } = 100;
    public int AutoCount { get; set; }
    public int AutoJitterPx { get; set; }
}

internal static class MacroStore
{
    public const string Extension = ".cfmacro";

    public static readonly string AppDataDirectory = ResolveDataDirectory();

    public static readonly string LastMacroPath = Path.Combine(AppDataDirectory, "last-session.cfmacro");

    private static readonly string SettingsPath = Path.Combine(AppDataDirectory, "settings.json");

    private static readonly JsonSerializerOptions FileOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public static void Save(string path, MacroDocument document)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(document, FileOptions);
        var temp = fullPath + ".tmp";
        File.WriteAllText(temp, json, new UTF8Encoding(false));
        File.Move(temp, fullPath, true);
    }

    public static MacroDocument Load(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var document = JsonSerializer.Deserialize<MacroDocument>(json, FileOptions)
            ?? throw new InvalidDataException("文件内容不是有效的轻录宏。");

        document.Events ??= new List<MacroEvent>();
        document.Events = document.Events
            .Where(e => e != null)
            .OrderBy(e => e.T)
            .ToList();

        if (document.Events.Count == 0)
        {
            throw new InvalidDataException("这个宏里没有任何操作。");
        }

        if (string.IsNullOrWhiteSpace(document.Name))
        {
            document.Name = Path.GetFileNameWithoutExtension(path);
        }

        return document;
    }

    public static void SaveLastSession(MacroDocument document)
    {
        if (document.IsEmpty)
        {
            return;
        }

        Directory.CreateDirectory(AppDataDirectory);
        Save(LastMacroPath, document);
    }

    public static MacroDocument? TryLoadLastSession()
    {
        try
        {
            if (!File.Exists(LastMacroPath))
            {
                return null;
            }

            return Load(LastMacroPath);
        }
        catch
        {
            return null;
        }
    }

    public static AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppSettings>(json, FileOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            var json = JsonSerializer.Serialize(settings, FileOptions);
            var temp = SettingsPath + ".tmp";
            File.WriteAllText(temp, json, new UTF8Encoding(false));
            File.Move(temp, SettingsPath, true);
        }
        catch
        {
            // Settings are nice-to-have; never block closing the app for them.
        }
    }

    private static string ResolveDataDirectory()
    {
        var overridePath = Environment.GetEnvironmentVariable("CLICKFLOW_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var portableFlag = Path.Combine(AppContext.BaseDirectory, "portable.flag");
        if (File.Exists(portableFlag))
        {
            return Path.Combine(AppContext.BaseDirectory, "data");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClickFlow");
    }

    public static string GetDefaultSaveDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var preferred = Path.Combine(documents, "ClickFlow");
        try
        {
            Directory.CreateDirectory(preferred);
            return preferred;
        }
        catch
        {
            Directory.CreateDirectory(AppDataDirectory);
            return AppDataDirectory;
        }
    }
}
