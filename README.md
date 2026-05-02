# CopyTrail

A modern, visual clipboard history app for Windows. Every clipboard entry shows where it came from, what type of content it is, and a clean visual preview — styled to reflect the source application.

## Features

- **Alt+V** to open the clipboard popup (does not intercept Windows+V)
- Card-based clipboard history with source app attribution
- Per-source app color accent and icon
- 16 content types detected: Text, URL, Code, JSON, HTML, Markdown, Rich Text, Image, Screenshot, SVG, Color, Terminal Command, File Reference, and more
- Search and filter by content type
- Pin items to keep them at the top
- Privacy exclusions — exclude apps from ever being recorded
- Capture pause (until resumed, 5 minutes, or 1 hour)
- Local SQLite storage — no cloud, no accounts, no telemetry
- System tray integration

## Requirements

- Windows 10 or later
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## Building from Source

```
dotnet build CopyTrail.sln
dotnet run --project src/CopyTrail/CopyTrail.csproj
```

## Running Tests

```
dotnet test CopyTrail.sln
```

## Privacy

CopyTrail is entirely local. No data ever leaves your machine.

- All history is stored in `%LOCALAPPDATA%\CopyTrail\CopyTrail.db`
- Settings are stored in `%APPDATA%\CopyTrail\settings.json`
- Images are stored in `%LOCALAPPDATA%\CopyTrail\images\`
- CopyTrail never records its own clipboard operations
- Password managers are excluded by default (1Password, Bitwarden, KeePass, KeePassXC, CredentialUIBroker)
- Additional apps can be excluded in Settings → Privacy

## Usage

| Action | How |
|---|---|
| Open popup | Alt+V |
| Paste item | Click a card |
| Copy without pasting | Ctrl+Enter |
| Navigate cards | Arrow keys |
| Close popup | Esc |
| Ignore an app | Click ⋯ on a card → "Ignore this app next time" |
| Pause capture | System tray → Pause Capture |
| Settings | System tray → Settings |

## Troubleshooting

### Shortcut does not work (Alt+V does nothing)

Another application may have already registered the Alt+V hotkey.

1. Check the log file at `%LOCALAPPDATA%\CopyTrail\logs\CopyTrail.log` for a line containing `Failed to register Alt+V hotkey`.
2. Close any app that intercepts Alt+V (some video conferencing tools, gaming overlays, or keyboard remappers do this).
3. Re-launch CopyTrail. The tray icon tooltip will confirm whether the hotkey is active.

### App does not capture clipboard changes

1. Verify capture is not paused — right-click the tray icon and check the menu. The icon tooltip says "Capture paused" if paused.
2. Check whether the source app is in the exclusion list: **Settings → Privacy → Excluded apps**.
3. If the source app is a password manager (1Password, Bitwarden, KeePass, KeePassXC), it is excluded by default and will never be captured.

### Popup does not appear

1. Confirm no other app is using Alt+V (see _Shortcut does not work_ above).
2. If the popup opens off-screen after a monitor arrangement change, try moving your mouse to your primary monitor before pressing Alt+V.
3. Restart CopyTrail from the system tray (right-click → Exit, then relaunch).

### Reset the database

To clear all clipboard history and start fresh:

1. Open CopyTrail settings (tray icon → Settings → scroll to Data section → **Clear All History**), **or**
2. Quit CopyTrail (tray → Exit), delete `%LOCALAPPDATA%\CopyTrail\CopyTrail.db` (and the `-shm`/`-wal` sibling files if present), then relaunch.

### Privacy exclusions

To stop CopyTrail from recording a specific application:

1. Copy something from the app you want to exclude.
2. Find the card in the popup, click the **⋯** button, then choose **Ignore this app next time**.
3. Alternatively, open **Settings → Privacy → Excluded apps** and type the process name (e.g. `notepad` or `notepad.exe`).

Excluded apps are matched case-insensitively and with or without the `.exe` suffix.

## Tech Stack

- C# / WPF
- .NET 10 (Windows Desktop SDK)
- SQLite via Microsoft.Data.Sqlite
- xUnit for tests

## Project Structure

```
src/CopyTrail/          Main application
  Controls/             ClipCard, SourceBadge user controls
  Converters/           WPF value converters
  Data/                 DbContext, repository, schema
  Helpers/              Win32 P/Invoke declarations
  Models/               Domain records and enums
  Services/             Clipboard, tray, hotkey, settings, exclusion services
  Utilities/            Content detectors, app name mapper, image utilities
  ViewModels/           INPC view models
  Views/                PopupWindow, SettingsWindow
tests/CopyTrail.Tests/  Unit tests (214 tests)
docs/ai/                AI agent working docs
```

## License

MIT
