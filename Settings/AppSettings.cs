using System.Text.Json;

namespace POE2TradeHelper.Settings;

public class AppSettings
{
    public string ClientLogPath { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile 2\logs\Client.txt";
    public string DiscordWebhook { get; set; } = string.Empty;
    public bool EnableSoundNotification { get; set; } = true;
    public bool EnableDiscordNotification { get; set; } = false;

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "POE2TradeHelper"
    );

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Fail silently - if we can't save settings, we'll just use defaults next time
        }
    }
}
