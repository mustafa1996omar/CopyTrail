# Privacy Policy

CopyTrail is a local-only Windows application. This document explains exactly what data it stores, where it stores it, and what it never does.

---

## What CopyTrail Stores

All data is stored **only on your machine**. Nothing is ever sent to the internet.

| Data | Location |
|------|----------|
| Clipboard history (text, URLs, etc.) | `%LOCALAPPDATA%\CopyTrail\CopyTrail.db` |
| Clipboard images and screenshots | `%LOCALAPPDATA%\CopyTrail\images\` |
| App settings | `%APPDATA%\CopyTrail\settings.json` |
| App icon cache | `%LOCALAPPDATA%\CopyTrail\icons\` |
| Log file | `%LOCALAPPDATA%\CopyTrail\logs\CopyTrail.log` |

---

## What CopyTrail Never Does

- **No accounts.** CopyTrail has no login, no registration, and no user identity system.
- **No telemetry.** CopyTrail never reports usage data, crash reports, or any analytics to any server.
- **No network calls.** CopyTrail makes zero outbound network connections. It has no server-side component.
- **No cloud sync.** Your clipboard history stays on your machine and is never uploaded anywhere.

---

## Excluded Apps Are Never Captured

If an application is on the exclusion list, CopyTrail will silently ignore all clipboard events from that application. No data from excluded apps is written to disk.

**Apps excluded by default:**
- 1Password
- Bitwarden
- KeePass
- KeePassXC
- CredentialUIBroker (Windows credential dialogs)

You can add more apps in **Settings → Privacy → Excluded apps**.

---

## Clearing Your History

You can delete all stored clipboard data at any time:

- In the app: **System tray → Settings → Data → Clear All History**
- Manually: Quit CopyTrail (tray → Exit), then delete `%LOCALAPPDATA%\CopyTrail\CopyTrail.db` and the `images\` folder.

---

## Opening the Data Folder

To see exactly what is on disk, open **Settings → Data → Open Data Folder**. This opens `%LOCALAPPDATA%\CopyTrail\` in Windows Explorer. You can delete any files there directly.

---

## Log Files

CopyTrail writes a log to `%LOCALAPPDATA%\CopyTrail\logs\CopyTrail.log` for troubleshooting purposes. The log records events such as service start, hotkey registration, and errors. **The log never records the content of clipboard entries.** Log files rotate automatically at 5 MB maximum and only the two most recent files are kept.

---

## Self-Exclusion

CopyTrail excludes its own process from clipboard capture. Operations CopyTrail performs (such as pasting an item) are never re-captured and stored as new history entries.

---

## Summary

CopyTrail is entirely local. It stores what you copy, on your machine, for your use. You are in full control of your data and can clear it or inspect it at any time.
