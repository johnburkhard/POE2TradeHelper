# POE2 Trade Helper

A lightweight Windows application that monitors Path of Exile 2 trade messages and provides instant notifications through sound alerts and Discord webhooks.

## Features

- Monitors POE2 Client.txt log file for trade messages
- Sound notifications when trade messages are detected
- Discord webhook integration for trade notifications
- Minimizes to system tray for unobtrusive operation
- Fully self-contained single executable
- Configurable settings that persist between sessions

## Requirements

To run the application:
- Windows operating system
- .NET runtime is not required (application is self-contained)

To compile from source:
- .NET 9.0 SDK or later
- Visual Studio 2022 or later (optional, can use command line)
- Windows operating system

## Installation

1. Download the latest release from the releases page
2. Run the executable - no installation required
3. Configure your Path of Exile 2 client log path
4. (Optional) Configure Discord webhook URL for notifications

## Building from Source

1. Clone the repository
2. Open a command prompt in the project directory
3. Run the following command:
```powershell
dotnet publish -c Release
```

The compiled executable will be in `bin\Release\net9.0-windows\win-x64\publish\POE2TradeHelper.exe`

## Configuration

Settings are stored in:
`%LOCALAPPDATA%\POE2TradeHelper\settings.json`

The settings file is automatically created when you first change settings. Default settings are:
- Sound notifications: Enabled
- Discord notifications: Disabled
- Client log path: Default Steam installation path
- Discord webhook: Empty

## Usage

1. Start the application
2. Configure the Path of Exile 2 client log path (if different from default)
3. (Optional) Add a Discord webhook URL for notifications
4. Click "Start Monitoring"
5. The application will minimize to system tray and monitor for trade messages
6. Double-click the tray icon to show the main window

## Dependencies

- NAudio (2.2.1) - For sound playback

## File Locations

- Executable: Single self-contained .exe file
- Settings: `%LOCALAPPDATA%\POE2TradeHelper\settings.json`
- Logs: All logs are displayed in the application window only

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Support

This is a community project. For support:
1. Check existing GitHub issues
2. Create a new issue with detailed information about your problem
3. Include log messages and steps to reproduce

## Acknowledgments

- Path of Exile 2 is a trademark of Grinding Gear Games
- Icon and sound resources are used under fair use for non-commercial purposes
