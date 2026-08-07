using System.IO;
using System.Text.Json;

namespace DropLAN;

public static class SettingsStore
{
    private sealed class SettingsModel
    {
        public string? DownloadFolder { get; set; }
    }

    private static string SettingsPath
    {
        get
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DropLAN");

            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "settings.json");
        }
    }

    public static string LoadDownloadFolder()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<SettingsModel>(json);

                if (!string.IsNullOrWhiteSpace(settings?.DownloadFolder))
                    return settings.DownloadFolder;
            }
        }
        catch
        {
            // Jeśli plik ustawień jest uszkodzony, wracamy do domyślnej lokalizacji.
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "DropLAN");
    }

    public static void SaveDownloadFolder(string folder)
    {
        var settings = new SettingsModel
        {
            DownloadFolder = folder
        };

        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(SettingsPath, json);
    }
}
