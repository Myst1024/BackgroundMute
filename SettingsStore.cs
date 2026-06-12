using System.Text.Json;

namespace BackgroundMute;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDir = Path.Combine(appData, "BackgroundMute");
        Directory.CreateDirectory(settingsDir);
        _settingsPath = Path.Combine(settingsDir, "settings.json");
    }

    public HashSet<string> LoadSelectedPrograms()
    {
        if (!File.Exists(_settingsPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings?.SelectedProgramKeys is null)
            {
                return [];
            }

            return new HashSet<string>(settings.SelectedProgramKeys, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }

    public void SaveSelectedPrograms(HashSet<string> selectedProgramKeys)
    {
        var settings = new AppSettings
        {
            SelectedProgramKeys = selectedProgramKeys.OrderBy(static x => x).ToArray()
        };

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private sealed class AppSettings
    {
        public string[] SelectedProgramKeys { get; set; } = [];
    }
}
