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
