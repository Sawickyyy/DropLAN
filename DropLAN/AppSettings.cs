using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace DropLAN;

public sealed class AppSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DropLAN");

    private static readonly string SettingsPath = Path.Combine(
        SettingsDirectory,
        "settings.json");

    public string? Language { get; set; }
    public string Theme { get; set; } = "system";
    public bool MinimizeToTray { get; set; } = true;
    public bool TransferNotifications { get; set; } = true;
    public bool AutoCheckUpdates { get; set; } = true;

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);

        var json = JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(SettingsPath, json);
    }

    public static void ApplyLanguage(string language)
    {
        var normalized = language.Equals(
            "pl",
            StringComparison.OrdinalIgnoreCase)
            ? "pl"
            : "en";

        var cultureName = normalized == "pl"
            ? "pl-PL"
            : "en-GB";

        var culture = CultureInfo.GetCultureInfo(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var app = System.Windows.Application.Current;
        if (app == null)
            return;

        var oldDictionary = app.Resources.MergedDictionaries
            .FirstOrDefault(dictionary =>
                dictionary.Source?.OriginalString.Contains(
                    "Localization/Strings.",
                    StringComparison.OrdinalIgnoreCase) == true);

        if (oldDictionary != null)
            app.Resources.MergedDictionaries.Remove(oldDictionary);

        app.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(
                    $"Localization/Strings.{normalized}.xaml",
                    UriKind.Relative)
            });
    }
}
