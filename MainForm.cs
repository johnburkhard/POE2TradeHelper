using NAudio.Wave;
using System.Text.RegularExpressions;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using POE2TradeHelper.Settings;

namespace POE2TradeHelper
{
    public partial class MainForm : Form
    {
        // Path to our notification sound in the embedded resources
        private const string SOUND_FILE = "POE2TradeHelper.Resources.trade_alert.mp3";
        
        // Matches POE2 trade messages like "@From PlayerName: Hi, I would like to buy your item..."
        private static readonly Regex TradeMessageRegex = new(@"@From (.+?): (.*I would like to buy.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // UI and system tray components
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private RichTextBox logBox = null!;
        private Button btnToggleMonitoring = null!;
        private Button btnClearLog = null!;
        private Button btnSave = null!;
        private Button btnExit = null!;

        // File monitoring state
        private System.Windows.Forms.Timer? pollTimer;
        private long lastPosition = 0;  // Tracks where we last read in the log file
        private bool isMonitoring = false;

        // Audio playback components
        private WaveOutEvent? waveOut;
        private AudioFileReader? audioFile;
        
        // User settings
        private AppSettings settings;

        public MainForm()
        {
            InitializeComponent();
            settings = AppSettings.Load();
            InitializeUI();
            SetupTrayIcon();
            ValidateRequirements();
        }

        // Load saved settings into the UI fields
        private void InitializeUI()
        {
            txtClientPath.Text = settings.ClientLogPath;
            txtWebhook.Text = settings.DiscordWebhook;
            chkSoundNotify.Checked = settings.EnableSoundNotification;
            chkDiscordNotify.Checked = settings.EnableDiscordNotification;
        }

        // Make sure we have everything we need to run:
        // - Access to POE2 client log
        // - Sound file for notifications
        // - Valid Discord webhook (if enabled)
        private void ValidateRequirements()
        {
            LogMessage("Initializing POE2 Trade Helper...");

            // First, check if we can read the POE2 log file
            if (string.IsNullOrEmpty(settings.ClientLogPath))
            {
                LogMessage("ERROR: Client log path is not configured");
                return;
            }

            if (!File.Exists(settings.ClientLogPath))
            {
                LogMessage($"ERROR: Client log file not found at: {settings.ClientLogPath}");
                return;
            }

            try
            {
                using var fs = File.Open(settings.ClientLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                LogMessage("✓ Successfully connected to Path of Exile 2 log file");
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Cannot access Path of Exile 2 log file: {ex.Message}");
                return;
            }

            // Check if our notification sound is available
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(SOUND_FILE);
            if (stream == null)
            {
                LogMessage("ERROR: Sound file not found in embedded resources");
                return;
            }
            else
            {
                LogMessage("✓ Sound file found");
            }

            // Validate Discord webhook if notifications are enabled
            if (string.IsNullOrEmpty(settings.DiscordWebhook))
            {
                LogMessage("ERROR: Discord webhook URL not configured");
                return;
            }

            // Basic format check for Discord webhook - we can't test it without sending a message
            if (!settings.DiscordWebhook.StartsWith("https://discord.com/api/webhooks/"))
            {
                LogMessage("ERROR: Invalid Discord webhook URL format");
                return;
            }
            
            LogMessage("✓ Discord webhook URL configured");
            LogMessage("Ready to start monitoring. Click 'Start Monitoring' to begin.");
        }

        // Save and validate user settings, making sure we can still access everything we need
        private void SaveSettings(object? sender, EventArgs e)
        {
            // Stop monitoring while we update settings
            if (isMonitoring)
            {
                StopMonitoring();
                LogMessage("Monitoring stopped to apply new settings", Color.Red);
            }

            // Keep track of what changed so we can log it
            var oldWebhook = settings.DiscordWebhook;
            var oldLogPath = settings.ClientLogPath;
            var oldSoundEnabled = settings.EnableSoundNotification;
            var oldDiscordEnabled = settings.EnableDiscordNotification;

            // Make sure we can actually read the log file before saving the path
            var newLogPath = txtClientPath.Text.Trim();
            if (!File.Exists(newLogPath))
            {
                MessageBox.Show($"Client log file not found at: {newLogPath}\nPlease check the path and try again.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using var fs = File.Open(newLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception)
            {
                MessageBox.Show($"Cannot access client log file at: {newLogPath}\nPlease check file permissions and try again.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Save all the new settings
            settings.ClientLogPath = newLogPath;
            settings.DiscordWebhook = txtWebhook.Text.Trim();
            settings.EnableSoundNotification = chkSoundNotify.Checked;
            settings.EnableDiscordNotification = chkDiscordNotify.Checked;
            settings.Save();

            // Update UI with cleaned up values
            txtClientPath.Text = settings.ClientLogPath;
            txtWebhook.Text = settings.DiscordWebhook;

            // Let the user know what changed
            if (oldLogPath != settings.ClientLogPath)
                LogMessage($"Client log path updated to: {settings.ClientLogPath}", Color.Cyan);
            if (oldWebhook != settings.DiscordWebhook)
                LogMessage("Discord webhook URL updated", Color.Cyan);
            if (oldSoundEnabled != settings.EnableSoundNotification)
                LogMessage($"Sound notifications {(settings.EnableSoundNotification ? "enabled" : "disabled")}", Color.Cyan);
            if (oldDiscordEnabled != settings.EnableDiscordNotification)
                LogMessage($"Discord notifications {(settings.EnableDiscordNotification ? "enabled" : "disabled")}", Color.Cyan);
            
            LogMessage("Settings saved successfully");
            MessageBox.Show("Settings saved successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Make sure everything still works with the new settings
            LogMessage("Validating new settings...");
            ValidateRequirements();

            if (isMonitoring)
                LogMessage("Click 'Start Monitoring' to resume monitoring with new settings");
        }

        // Send a trade notification to Discord using their webhook API
        private async void SendDiscordNotification(string message)
        {
            if (!settings.EnableDiscordNotification)
                return;

            try
            {
                var webhookUrl = settings.DiscordWebhook;
                using var client = new HttpClient();

                // Create a nice looking Discord embed with our trade message
                var message_data = new
                {
                    username = "POE2 Trade Helper",
                    avatar_url = "https://raw.githubusercontent.com/johnburkhard/POE2TradeHelper/public-release/Resources/trade-icon.png",
                    embeds = new[]
                    {
                        new
                        {
                            title = "New Trade Request",
                            description = message,
                            color = 3447003, // Discord's blue color
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(message_data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync(webhookUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    LogMessage($"Error sending Discord notification: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error sending Discord notification: {ex.Message}");
            }
        }

        // Play our trade notification sound using NAudio
        private void PlayNotificationSound()
        {
            try
            {
                if (!settings.EnableSoundNotification)
                    return;

                // Get our embedded sound file
                using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(SOUND_FILE);
                if (stream == null)
                {
                    LogMessage("ERROR: Sound file not found in embedded resources");
                    return;
                }

                // NAudio needs a real file, so we'll make a temporary one
                var tempFile = Path.GetTempFileName();
                using (var fileStream = File.Create(tempFile))
                {
                    stream.CopyTo(fileStream);
                }

                // Clean up any previous sound that was playing
                waveOut?.Stop();
                waveOut?.Dispose();
                audioFile?.Dispose();

                // Play the new sound
                audioFile = new AudioFileReader(tempFile);
                waveOut = new WaveOutEvent();
                waveOut.Init(audioFile);
                waveOut.Play();

                // Clean up our temp file after a second
                Task.Delay(1000).ContinueWith(_ => 
                {
                    try { File.Delete(tempFile); } 
                    catch { /* ignore cleanup errors */ }
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error playing sound: {ex.Message}");
            }
        }

        // Set up the system tray icon and its right-click menu
        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("Start Monitoring", null, (s, e) => StartMonitoring());
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon
            {
                Icon = Properties.Resources.trade_icon,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "POE2 Trade Helper"
            };

            // Double-clicking the tray icon shows the main window
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

            // Hide to tray instead of closing when user clicks X
            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

        // Start/stop monitoring when the user clicks the button
        private void ToggleMonitoring(object? sender, EventArgs e)
        {
            if (isMonitoring)
            {
                StopMonitoring();
            }
            else
            {
                StartMonitoring();
            }
        }

        // Start monitoring the POE2 log file for trade messages
        private void StartMonitoring()
        {
            if (isMonitoring) return;

            try
            {
                // Start reading from the end of the file
                using (var fs = new FileStream(settings.ClientLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    lastPosition = fs.Length;
                    LogMessage($"Starting to monitor from position: {lastPosition}");
                }

                // Check for new messages every second
                pollTimer = new System.Windows.Forms.Timer();
                pollTimer.Interval = 1000;
                pollTimer.Tick += (s, e) => CheckForNewContent();
                pollTimer.Start();

                // Update UI to show we're monitoring
                isMonitoring = true;
                btnToggleMonitoring.Text = "Stop Monitoring";
                if (trayMenu?.Items.Count > 1)
                    trayMenu.Items[1].Text = "Stop Monitoring";

                LogMessage("Monitoring started", Color.Lime);
            }
            catch (Exception ex)
            {
                LogMessage($"Error starting monitoring: {ex.Message}");
            }
        }

        // Stop monitoring the POE2 log file
        private void StopMonitoring()
        {
            if (!isMonitoring) return;

            // Clean up the timer
            if (pollTimer != null)
            {
                pollTimer.Stop();
                pollTimer.Dispose();
                pollTimer = null;
            }

            // Update UI to show we've stopped
            isMonitoring = false;
            btnToggleMonitoring.Text = "Start Monitoring";
            if (trayMenu?.Items.Count > 1)
                trayMenu.Items[1].Text = "Start Monitoring";

            LogMessage("Monitoring stopped", Color.Red);
        }

        // Check the log file for new trade messages
        private void CheckForNewContent()
        {
            try
            {
                using var fs = new FileStream(settings.ClientLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                // Handle case where log file was cleared/reset
                if (lastPosition > fs.Length)
                {
                    LogMessage("File was truncated, resetting position");
                    lastPosition = 0;
                }

                // Only read if there's new content
                if (fs.Length > lastPosition)
                {
                    using var reader = new StreamReader(fs);
                    fs.Seek(lastPosition, SeekOrigin.Begin);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Look for trade messages in each new line
                        var match = TradeMessageRegex.Match(line);
                        
                        if (match.Success)
                        {
                            var player = match.Groups[1].Value;
                            var message = match.Groups[2].Value;

                            var tradeMessage = $"From {player}: {message}";
                            LogMessage($"Trade message detected: {tradeMessage}");
                            
                            // Send notifications based on user settings
                            if (settings.EnableSoundNotification)
                                PlayNotificationSound();
                                
                            if (settings.EnableDiscordNotification)
                                SendDiscordNotification(tradeMessage);
                        }
                    }

                    lastPosition = fs.Position;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error reading file: {ex.Message}");
            }
        }

        // Add a timestamped message to our log window with optional color
        private void LogMessage(string message, Color? color = null)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => LogMessage(message, color)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            
            // Color-code different types of messages
            if (color != null)
            {
                logBox.SelectionColor = color.Value;
            }
            else if (message.StartsWith("ERROR:"))
            {
                logBox.SelectionColor = Color.Red;
            }
            else if (message.StartsWith("✓"))
            {
                logBox.SelectionColor = Color.Green;
            }
            else if (message.StartsWith("Trade message detected:"))
            {
                logBox.SelectionColor = Color.Yellow;
            }
            else
            {
                logBox.SelectionColor = Color.Gray;
            }

            logBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        // Helper to create an icon from a PNG file
        private Icon CreateIconFromPng(string iconPath)
        {
            using var bitmap = new Bitmap(iconPath);
            return Icon.FromHandle(bitmap.GetHicon());
        }

        // Clean up all our resources when closing
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopMonitoring();
                trayIcon?.Dispose();
                waveOut?.Dispose();
                audioFile?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
