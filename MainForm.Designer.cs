using System.Drawing;
using System.IO;

namespace POE2TradeHelper
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private TextBox txtClientPath;
        private Button btnBrowse;
        private TextBox txtWebhook;
        private CheckBox chkDiscordNotify;
        private CheckBox chkSoundNotify;
        private Panel settingsPanel;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            
            // Form settings
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Size = new System.Drawing.Size(1000, 750);
            this.Text = "POE2 Trade Helper";
            this.Icon = Properties.Resources.trade_icon;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

            // Initialize main content panel
            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 11),
                Margin = new Padding(10)
            };

            // Create settings panel
            settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(20)
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) }
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Client.txt Section
            var clientSection = new GroupBox
            {
                Text = "Game Log File Location",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };

            var clientLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };
            clientLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var pathPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 30,
                Margin = new Padding(0, 0, 0, 5),
                MinimumSize = new Size(750, 30)
            };
            clientLayout.Controls.Add(pathPanel, 0, 0);

            txtClientPath = new TextBox 
            { 
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11),
                AutoSize = true,
                MinimumSize = new Size(750, 25)
            };
            txtClientPath.Enter += (s, e) => {
                pathPanel.MaximumSize = new Size(0, 30);  
            };
            txtClientPath.Leave += (s, e) => {
                pathPanel.MaximumSize = new Size(750, 30);  
            };
            pathPanel.Controls.Add(txtClientPath);

            var browsePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 30,
                Margin = new Padding(0, 0, 0, 5)
            };
            clientLayout.Controls.Add(browsePanel, 0, 1);

            btnBrowse = new Button 
            { 
                Text = "Browse...",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 8.25f),
                UseVisualStyleBackColor = true,
                Padding = new Padding(3, 0, 3, 0)
            };
            btnBrowse.Click += (s, e) => {
                var dialog = new OpenFileDialog
                {
                    Filter = "Text files|*.txt|All files|*.*",
                    Title = "Select Client.txt file"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtClientPath.Text = dialog.FileName;
                }
            };
            browsePanel.Controls.Add(btnBrowse);

            var lblClientHelp = new Label
            {
                Text = "This file contains the game chat logs and is typically located in the Path of Exile 2 installation folder.",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10),
                ForeColor = Color.FromArgb(64, 64, 64),  // Darker gray
                Margin = new Padding(0, 5, 0, 0)
            };
            clientLayout.Controls.Add(lblClientHelp, 0, 2);

            clientSection.Controls.Add(clientLayout);
            mainLayout.Controls.Add(clientSection, 0, 0);

            // Discord Section
            var discordSection = new GroupBox
            {
                Text = "Discord Integration",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(0, 10, 0, 0),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };

            var discordLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true
            };

            var webhookPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 30,
                Margin = new Padding(0),
                MinimumSize = new Size(750, 30)
            };

            txtWebhook = new TextBox 
            { 
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11),
                AutoSize = true,
                MinimumSize = new Size(750, 25)
            };
            txtWebhook.Enter += (s, e) => {
                webhookPanel.MaximumSize = new Size(0, 30);  
            };
            txtWebhook.Leave += (s, e) => {
                webhookPanel.MaximumSize = new Size(750, 30);  
            };
            webhookPanel.Controls.Add(txtWebhook);
            discordLayout.Controls.Add(webhookPanel, 0, 0);

            var lblWebhookHelp = new Label
            {
                Text = "To set up Discord notifications, you'll need to create a webhook in your Discord server:\n" +
                      "1. Open your Discord server settings\n" +
                      "2. Click on 'Integrations'\n" +
                      "3. Click on 'Create Webhook'\n" +
                      "4. Copy the webhook URL and paste it here",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10),
                ForeColor = Color.FromArgb(64, 64, 64),  // Darker gray
                Margin = new Padding(0, 0, 0, 5)
            };
            discordLayout.Controls.Add(lblWebhookHelp, 0, 1);

            var lblWebhookDocs = new LinkLabel
            {
                Text = "Need help? View Discord's webhook documentation",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10)
            };
            lblWebhookDocs.Click += (s, e) => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                    UseShellExecute = true
                });
            };
            discordLayout.Controls.Add(lblWebhookDocs, 0, 2);

            discordSection.Controls.Add(discordLayout);
            mainLayout.Controls.Add(discordSection, 0, 1);

            // Notifications Section
            var notifySection = new GroupBox
            {
                Text = "Notification Settings",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(0, 10, 0, 0),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                MinimumSize = new Size(0, 100)
            };

            var notifyLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 5, 0, 0)
            };

            chkDiscordNotify = new CheckBox
            {
                Text = "Send notifications to Discord",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10),
                Margin = new Padding(0, 0, 0, 8)
            };
            notifyLayout.Controls.Add(chkDiscordNotify);

            chkSoundNotify = new CheckBox
            {
                Text = "Play sound notification",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10),
                Margin = new Padding(0, 0, 0, 0)
            };
            notifyLayout.Controls.Add(chkSoundNotify);

            notifySection.Controls.Add(notifyLayout);
            mainLayout.Controls.Add(notifySection, 0, 2);

            settingsPanel.Controls.Add(mainLayout);

            // Bottom buttons panel
            var bottomButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10),
                AutoSize = true
            };

            btnToggleMonitoring = new Button
            {
                Text = "Start Monitoring",
                Location = new Point(10, 10),
                Width = 120,
                Height = 30,
                Visible = true
            };
            btnToggleMonitoring.Click += ToggleMonitoring;

            btnClearLog = new Button
            {
                Text = "Clear Log",
                Location = new Point(140, 10),
                Width = 120,
                Height = 30,
                Visible = true
            };
            btnClearLog.Click += (s, e) => {
                logBox.Clear();
                LogMessage("Log cleared");
            };

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(10, 10),
                Width = 120,
                Height = 30,
                Visible = false
            };
            btnSave.Click += SaveSettings;

            btnExit = new Button
            {
                Text = "Exit",
                Anchor = AnchorStyles.Right,
                Location = new Point(bottomButtonPanel.Width - 130, 10),
                Width = 120,
                Height = 30
            };
            btnExit.Click += (s, e) => Application.Exit();

            var btnSettings = new Button
            {
                Text = "Settings",
                Anchor = AnchorStyles.Right,
                Location = new Point(bottomButtonPanel.Width - 260, 10),
                Width = 120,
                Height = 30
            };
            btnSettings.Click += (s, e) => {
                logBox.Visible = !logBox.Visible;
                settingsPanel.Visible = !settingsPanel.Visible;
                btnSettings.Text = logBox.Visible ? "Settings" : "Monitor";
                btnToggleMonitoring.Visible = logBox.Visible;
                btnClearLog.Visible = logBox.Visible;
                btnSave.Visible = !logBox.Visible;
            };

            bottomButtonPanel.Controls.AddRange(new Control[] { btnToggleMonitoring, btnClearLog, btnSave, btnSettings, btnExit });
            this.Controls.Add(settingsPanel);
            this.Controls.Add(logBox);
            this.Controls.Add(bottomButtonPanel);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        #endregion
    }
}
