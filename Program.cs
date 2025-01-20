namespace POE2TradeHelper;

// Main application entry point and configuration
static class Program
{
    // Start up our Windows Forms application and show the main window
    [STAThread]
    static void Main()
    {
        // Set up Windows Forms with standard DPI awareness and other defaults
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    
}