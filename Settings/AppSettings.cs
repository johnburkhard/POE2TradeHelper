using System.Text.Json;

namespace POE2TradeHelper.Settings;

// Handles saving and loading user settings to a JSON file in AppData
public class AppSettings
{
    // Default path to POE2 client log - users can change this in the UI
    public string ClientLogPath { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile 2\logs\Client.txt";
    
    // Discord webhook URL for trade notifications
    public string DiscordWebhook { get; set; } = string.Empty;
    
    // Whether to play a sound when trade messages are received
    public bool EnableSoundNotification { get; set; } = true;
    
    // Whether to send trade messages to Discord
    public bool EnableDiscordNotification { get; set; } = false;

    // Store settings in AppData/Local/POE2TradeHelper
    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "POE2TradeHelper"
    );

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    // Load settings from disk, or return defaults if no saved settings exist
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
            // If settings file is corrupted, just start fresh
            return new AppSettings();
        }
    }

    // Save current settings to disk
    public void Save()
    {
        try
        {
            // Make sure our settings directory exists
            Directory.CreateDirectory(SettingsDirectory);
            
            // Save as nicely formatted JSON
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Fail silently - if we can't save settings, we'll just use defaults next time
        }
    }
}
