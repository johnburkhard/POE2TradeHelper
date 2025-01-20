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
        private const string SOUND_FILE = "POE2TradeHelper.Resources.trade_alert.mp3";
        private static readonly Regex TradeMessageRegex = new(@"@From (.+?): (.*I would like to buy.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private RichTextBox logBox = null!;
        private Button btnToggleMonitoring = null!;
        private Button btnClearLog = null!;
        private Button btnSave = null!;
        private Button btnExit = null!;
        private System.Windows.Forms.Timer? pollTimer;
        private long lastPosition = 0;
        private bool isMonitoring = false;
        private WaveOutEvent? waveOut;
        private AudioFileReader? audioFile;
        private AppSettings settings;

        public MainForm()
        {
            InitializeComponent();
            settings = AppSettings.Load();
            InitializeUI();
            ValidateRequirements();
        }

        private void InitializeUI()
        {
            // Load settings into UI
            txtClientPath.Text = settings.ClientLogPath;
            txtWebhook.Text = settings.DiscordWebhook;
            chkSoundNotify.Checked = settings.EnableSoundNotification;
            chkDiscordNotify.Checked = settings.EnableDiscordNotification;
        }

        private void ValidateRequirements()
        {
            LogMessage("Initializing POE2 Trade Helper...");

            // Check client log path
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

            // Check sound file
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

            // Check Discord webhook
            if (string.IsNullOrEmpty(settings.DiscordWebhook))
            {
                LogMessage("ERROR: Discord webhook URL not configured");
                return;
            }

            // Don't validate webhook URL - Discord only accepts POST requests
            // Just check if it's a valid Discord webhook URL format
            if (!settings.DiscordWebhook.StartsWith("https://discord.com/api/webhooks/"))
            {
                LogMessage("ERROR: Invalid Discord webhook URL format");
                return;
            }
            
            LogMessage("✓ Discord webhook URL configured");
            LogMessage("Ready to start monitoring. Click 'Start Monitoring' to begin.");
        }

        private void SaveSettings(object? sender, EventArgs e)
        {
            // If currently monitoring, stop it
            if (isMonitoring)
            {
                StopMonitoring();
                LogMessage("Monitoring stopped to apply new settings", Color.Red);
            }

            var oldWebhook = settings.DiscordWebhook;
            var oldLogPath = settings.ClientLogPath;
            var oldSoundEnabled = settings.EnableSoundNotification;
            var oldDiscordEnabled = settings.EnableDiscordNotification;

            // Validate client log path before saving
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

            settings.ClientLogPath = newLogPath;
            settings.DiscordWebhook = txtWebhook.Text.Trim();
            settings.EnableSoundNotification = chkSoundNotify.Checked;
            settings.EnableDiscordNotification = chkDiscordNotify.Checked;
            settings.Save();

            // Update the textboxes with trimmed values
            txtClientPath.Text = settings.ClientLogPath;
            txtWebhook.Text = settings.DiscordWebhook;

            // Log what changed
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

            LogMessage("Validating new settings...");
            ValidateRequirements();

            if (isMonitoring)
                LogMessage("Click 'Start Monitoring' to resume monitoring with new settings");
        }

        private async void SendDiscordNotification(string message)
        {
            if (!settings.EnableDiscordNotification)
                return;

            try
            {
                var webhookUrl = settings.DiscordWebhook;
                using var client = new HttpClient();

                // Convert the icon to a base64 string for the Discord message
                string avatar_url;
                using (var ms = new MemoryStream())
                {
                    Properties.Resources.trade_icon_png.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();
                    avatar_url = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
                }

                // Format the message into a nice Discord embed
                var embed = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = "New Trade Request",
                            description = message,
                            color = 3447003, // Blue color
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    },
                    username = "POE2 Trade Helper",
                    avatar_url = avatar_url
                };

                var json = System.Text.Json.JsonSerializer.Serialize(embed);
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

        private void PlayNotificationSound()
        {
            try
            {
                if (!settings.EnableSoundNotification)
                    return;

                using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(SOUND_FILE);
                if (stream == null)
                {
                    LogMessage("ERROR: Sound file not found in embedded resources");
                    return;
                }

                // Create a temporary file for NAudio
                var tempFile = Path.GetTempFileName();
                using (var fileStream = File.Create(tempFile))
                {
                    stream.CopyTo(fileStream);
                }

                // Stop and dispose any existing audio
                waveOut?.Stop();
                waveOut?.Dispose();
                audioFile?.Dispose();

                audioFile = new AudioFileReader(tempFile);
                waveOut = new WaveOutEvent();
                waveOut.Init(audioFile);
                waveOut.Play();

                // Delete temp file after a delay
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

            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

            // Form closing behavior
            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

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

        private void StartMonitoring()
        {
            if (isMonitoring) return;

            try
            {
                // Get current file position
                using (var fs = new FileStream(settings.ClientLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    lastPosition = fs.Length;
                    LogMessage($"Starting to monitor from position: {lastPosition}");
                }

                // Set up polling timer
                pollTimer = new System.Windows.Forms.Timer();
                pollTimer.Interval = 1000; // Check every second
                pollTimer.Tick += (s, e) => CheckForNewContent();
                pollTimer.Start();

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

        private void StopMonitoring()
        {
            if (!isMonitoring) return;

            if (pollTimer != null)
            {
                pollTimer.Stop();
                pollTimer.Dispose();
                pollTimer = null;
            }

            isMonitoring = false;
            btnToggleMonitoring.Text = "Start Monitoring";
            if (trayMenu?.Items.Count > 1)
                trayMenu.Items[1].Text = "Start Monitoring";

            LogMessage("Monitoring stopped", Color.Red);
        }

        private void CheckForNewContent()
        {
            try
            {
                using var fs = new FileStream(settings.ClientLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                if (lastPosition > fs.Length)
                {
                    LogMessage("File was truncated, resetting position");
                    lastPosition = 0;
                }

                if (fs.Length > lastPosition)
                {
                    using var reader = new StreamReader(fs);
                    fs.Seek(lastPosition, SeekOrigin.Begin);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = TradeMessageRegex.Match(line);
                        
                        if (match.Success)
                        {
                            var player = match.Groups[1].Value;
                            var message = match.Groups[2].Value;

                            var tradeMessage = $"From {player}: {message}";
                            LogMessage($"Trade message detected: {tradeMessage}");
                            
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

        private void LogMessage(string message, Color? color = null)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => LogMessage(message, color)));
                return;
            }

            // Add timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            
            // Set color based on message type or passed color
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

        private Icon CreateIconFromPng(string iconPath)
        {
            using var bitmap = new Bitmap(iconPath);
            return Icon.FromHandle(bitmap.GetHicon());
        }

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
