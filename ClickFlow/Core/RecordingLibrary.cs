using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClickFlow.UI;

namespace ClickFlow.Core;

/// <summary>
/// Persistent recording library. Only metadata is kept in memory; macro files
/// are loaded when a recording is expanded or played.
/// </summary>
internal static class RecordingLibrary
{
    private static readonly object Sync = new();
    private static readonly List<RecordingEntry> Entries = new();
    private static bool _loaded;

    private static readonly JsonSerializerOptions IndexOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FolderPath { get; } = Path.Combine(MacroStore.AppDataDirectory, "recordings");

    private static string IndexPath => Path.Combine(FolderPath, "library.json");

    public static IReadOnlyList<RecordingEntry> All
    {
        get
        {
            EnsureLoaded();
            lock (Sync)
            {
                return Entries.ToArray();
            }
        }
    }

    public static long TotalBytes
    {
        get
        {
            EnsureLoaded();
            lock (Sync)
            {
                return Entries.Sum(e => e.FileSize);
            }
        }
    }

    public static void EnsureLoaded()
    {
        lock (Sync)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            Directory.CreateDirectory(FolderPath);
            LoadIndex();
            MigrateLegacySession();
            ScanForNewFiles();
            RemoveMissingFiles();
            SortAndSaveIfNeeded();
        }
    }

    public static RecordingEntry Add(MacroDocument document)
    {
        EnsureLoaded();
        lock (Sync)
        {
            Directory.CreateDirectory(FolderPath);
            var id = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"rec-{DateTime.Now:yyyyMMdd-HHmmss}-{id}.cfmacro";
            var path = Path.Combine(FolderPath, fileName);

            if (string.IsNullOrWhiteSpace(document.Name))
            {
                document.Name = L.T(
                    $"录制 {DateTime.Now:MM-dd HH:mm}",
                    $"Recording {DateTime.Now:MM-dd HH:mm}");
            }

            document.Name = TrimName(document.Name);
            document.CreatedUtc = DateTime.UtcNow;
            MacroStore.Save(path, document);

            var display = DisplayActionBuilder.Build(document.Events, includePending: true);
            var entry = new RecordingEntry
            {
                Id = id,
                FileName = fileName,
                Name = document.Name,
                CreatedUtc = document.CreatedUtc,
                DurationMs = document.DurationMs,
                ActionCount = display.Summary.ActionCount,
                ClickCount = display.Summary.ClickCount,
                DragCount = display.Summary.DragCount,
                KeyCount = display.Summary.KeyCount,
                ScrollCount = display.Summary.ScrollCount,
                EventCount = document.Events.Count,
                FileSize = new FileInfo(path).Length
            };

            Entries.Insert(0, entry);
            SaveIndex();
            return entry;
        }
    }

    public static MacroDocument LoadDocument(RecordingEntry entry)
    {
        if (!File.Exists(entry.FilePath))
        {
            throw new FileNotFoundException(
                L.T("录制文件已经不存在。", "The recording file no longer exists."),
                entry.FilePath);
        }

        return MacroStore.Load(entry.FilePath);
    }

    public static void Delete(RecordingEntry entry)
    {
        EnsureLoaded();
        lock (Sync)
        {
            try
            {
                if (File.Exists(entry.FilePath))
                {
                    File.Delete(entry.FilePath);
                }
            }
            finally
            {
                Entries.RemoveAll(e => e.Id == entry.Id);
                SaveIndex();
            }
        }
    }

    public static RecordingEntry Import(string path)
    {
        EnsureLoaded();
        var document = MacroStore.Load(path);
        return Add(document);
    }

    public static void OpenFolder()
    {
        Directory.CreateDirectory(FolderPath);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = FolderPath,
            UseShellExecute = true
        });
    }

    private static void LoadIndex()
    {
        Entries.Clear();
        if (!File.Exists(IndexPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(IndexPath, Encoding.UTF8);
            var index = JsonSerializer.Deserialize<RecordingIndex>(json, IndexOptions);
            if (index?.Entries != null)
            {
                Entries.AddRange(index.Entries.Where(e =>
                    !string.IsNullOrWhiteSpace(e.FileName) &&
                    !string.IsNullOrWhiteSpace(e.Name)));
            }
        }
        catch
        {
            Entries.Clear();
        }
    }

    private static void MigrateLegacySession()
    {
        var legacyPaths = new[]
        {
            MacroStore.LastMacroPath,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClickFlow",
                "last-session.cfmacro")
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var path in legacyPaths)
        {
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var document = MacroStore.Load(path);
                if (document.Events.Count == 0)
                {
                    File.Delete(path);
                    continue;
                }

                document.Name = string.IsNullOrWhiteSpace(document.Name)
                    ? L.T("之前录制的宏", "Previous recording")
                    : document.Name;
                Add(document);
                File.Delete(path);
            }
            catch
            {
                // A broken legacy session must not block the library.
            }
        }
    }

    private static void ScanForNewFiles()
    {
        try
        {
            var known = Entries.Select(e => e.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.EnumerateFiles(FolderPath, "*.cfmacro"))
            {
                var fileName = Path.GetFileName(path);
                if (known.Contains(fileName))
                {
                    continue;
                }

                try
                {
                    var document = MacroStore.Load(path);
                    var display = DisplayActionBuilder.Build(document.Events, includePending: true);
                    Entries.Add(new RecordingEntry
                    {
                        Id = Path.GetFileNameWithoutExtension(fileName),
                        FileName = fileName,
                        Name = string.IsNullOrWhiteSpace(document.Name)
                            ? Path.GetFileNameWithoutExtension(fileName)
                            : document.Name,
                        CreatedUtc = document.CreatedUtc == default
                            ? File.GetLastWriteTimeUtc(path)
                            : document.CreatedUtc,
                        DurationMs = document.DurationMs,
                        ActionCount = display.Summary.ActionCount,
                        ClickCount = display.Summary.ClickCount,
                        DragCount = display.Summary.DragCount,
                        KeyCount = display.Summary.KeyCount,
                        ScrollCount = display.Summary.ScrollCount,
                        EventCount = document.Events.Count,
                        FileSize = new FileInfo(path).Length
                    });
                }
                catch
                {
                    // Ignore files that are not valid ClickFlow macros.
                }
            }
        }
        catch
        {
            // If the folder cannot be scanned, keep the index contents.
        }
    }

    private static void RemoveMissingFiles()
    {
        Entries.RemoveAll(e => !File.Exists(e.FilePath));
    }

    private static void SortAndSaveIfNeeded()
    {
        var sorted = Entries.OrderByDescending(e => e.CreatedUtc).ToList();
        var changed = !Entries.Select(e => e.Id).SequenceEqual(sorted.Select(e => e.Id));
        Entries.Clear();
        Entries.AddRange(sorted);
        if (changed || !File.Exists(IndexPath))
        {
            SaveIndex();
        }
    }

    private static void SaveIndex()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            var json = JsonSerializer.Serialize(
                new RecordingIndex { Entries = Entries },
                IndexOptions);
            var temp = IndexPath + ".tmp";
            File.WriteAllText(temp, json, new UTF8Encoding(false));
            File.Move(temp, IndexPath, true);
        }
        catch
        {
            // The next start can rebuild the index from the macro files.
        }
    }

    private static string TrimName(string name)
    {
        name = name.Trim();
        return name.Length <= 80 ? name : name[..77] + "…";
    }
}
