# CopyTrail — Paste-Style UI Redesign
**Date:** 2026-05-02  
**Status:** Awaiting user review  
**Scope:** PopupWindow, ClipCard, SettingsWindow (theme toggle only)

---

## Overview

Replace the current vertical-list popup with a full-width bottom panel inspired by the macOS Paste app. The panel slides up from the screen bottom, shows a single horizontal row of cards (newest → oldest), and dismisses with a slide-down animation. Dark, Light, and System theme modes are added to Settings.

---

## 1. Layout & Window

### Panel dimensions
- **Width:** 100% of the monitor's working area width (edge to edge)
- **Height:** Fixed at ~240px total (handle 14px + header 48px + card row 148px + footer 30px)
- **Position:** Anchored to the absolute bottom of the screen (`Top = screenHeight - panelHeight`)
- **WindowStyle:** None, AllowsTransparency=True, ShowInTaskbar=False, Topmost=True
- **ResizeMode:** NoResize

### Taskbar behaviour
The panel opens from the absolute bottom of the screen and **overlaps the Windows taskbar** while visible. This is intentional — the panel is a transient overlay used for a few seconds. When the user picks a clip or dismisses, the panel slides back down and the taskbar is fully visible again. No taskbar-height detection is performed.

### Multi-monitor
The panel always appears on the **monitor where the mouse cursor is at the moment Alt+V is pressed**. `System.Windows.Forms.Cursor.Position` is used to identify the correct screen via `Screen.FromPoint`.

---

## 2. Slide-Up / Slide-Down Animation

### Show (Alt+V pressed)
1. `Window.Show()` — window is already positioned at bottom, off-screen via `TranslateTransform`
2. `TranslateTransform.Y` starts at `+panelHeight`
3. Storyboard animates `Y → 0` in **240 ms**, `CubicEase Out`
4. Simultaneously: `Opacity` 0 → 1 over the first **80 ms**
5. Window is never closed between uses — stays resident and hidden for instant re-open

### Hide (Esc / focus lost / item pasted)
1. If triggered by Enter/click paste: paste action fires **before** animation starts
2. Storyboard animates `Y → +panelHeight` in **180 ms**, `CubicEase In`
3. Simultaneously: `Opacity` 1 → 0 over the last **60 ms** (starts at 120 ms)
4. `Window.Hide()` called in `Storyboard.Completed` handler

---

## 3. Panel Structure (top → bottom)

```
┌─────────────────────────────────── full screen width ───────────────────────────────────┐
│  ▬▬▬  (drag handle, decorative only)                                                    │
│  [CT] [Search clipboard history…]  [All][Text][Links][Code][Images][Colors][Files][📌]  ⚙│
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  ◀  [card][card][card][card][card][card][card][card]…  ▶                               │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  ← → Navigate cards  ·  Enter Paste  ·  Ctrl+Enter Copy only  ·  P Pin  ·  Esc Dismiss │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### Header row (48px)
- **Logo mark** — 26×26px rounded square, indigo gradient, "CT" text
- **Search box** — glass-style input, placeholder "Search clipboard history…", clear (×) button when active
- **Filter chips** — All / Text / Links / Code / Images / Colors / Files / 📌 Pinned (horizontally scrollable if needed)
- **Settings button** — ⚙ icon, opens SettingsWindow

### Card row (148px)
- Horizontal `ScrollViewer`, `HorizontalScrollBarVisibility=Hidden`
- **Scroll arrows** (◀ ▶) appear at left/right edges; ◀ is hidden when scrolled to the start, ▶ is hidden when scrolled to the end; clicking scrolls by one card width (176px)
- Mouse `scroll wheel` on the card area scrolls horizontally (WPF `PreviewMouseWheel` → `ScrollViewer.ScrollToHorizontalOffset`)
- 8px gap between cards, 20px padding left/right

### Card ordering
- **Pinned items first** (left), separated from unpinned by a 1px vertical divider
- **Unpinned items** follow, newest → oldest (left to right)
- When the 📌 Pinned chip is active, only pinned items are shown (no divider needed)

### Footer (30px)
Keyboard hint bar:  
`← →` Navigate cards · `Enter` Paste selected · `Ctrl+Enter` Copy only · `P` Pin / unpin · `Ctrl+F` Search · `Esc` Dismiss

---

## 4. Clip Card Design

### Dimensions
- Width: **168px** fixed
- Height: **auto** to fill the card row (approximately 138px: band 36px + body 72px + footer 30px)
- Corner radius: 11px
- Border: 1.5px, default `rgba(255,255,255,0.06)` dark / `rgba(0,0,0,0.07)` light

### Card band (top, 36px)
- **Background:** horizontal gradient tinted from the source app's accent colour
  - Dark mode: `linear-gradient(90deg, appColor @35% opacity, appColor @12% opacity)`
  - Light mode: `linear-gradient(90deg, appColor @12% opacity, appColor @4% opacity)`
- **App icon** (20×20px, rounded 5px) — fetched from `SourceIdentity.IconPath`; fallback = coloured circle with initial letter
- **App name** — 11px SemiBold, coloured to match app accent (lighter tint in dark, darker in light)
- **Content-kind badge** — 9px, pill, colour-coded per type (see below)

### Per-type content body (72px)
| Kind | Rendering |
|------|-----------|
| Text / RichText / Markdown | 11px body text, 5-line clamp |
| Code / JSON / Terminal | Dark monospace block (`#0c0a20` bg, `#a5f3fc` text) |
| Image / Screenshot | Thumbnail centred, `Stretch=Uniform` |
| URL / Link | Domain bold + full URL in monospace |
| Color | Swatch rectangle + hex value |
| File | File icon + filename |

### Card footer (30px)
- **Default state:** timestamp (left) + pin icon ⚑ (right, blue if pinned, dim if not) + menu ⋯
- **Mouse hover:** replaces footer with `↵ Paste` (accent colour) + `⎘ Copy` (muted) + 🗑 Delete (red on hover)
- **Keyboard selected:** replaces footer with `↵ Enter to paste` (accent) + `Ctrl+Enter copy` (muted)

### Card states
| State | Visual change |
|-------|--------------|
| Default | As described above |
| Hovered (mouse) | `translateY(-3px)`, border brightens, footer shows actions |
| Keyboard selected | `translateY(-4px)`, border white/bright, glow shadow, footer shows "Enter to paste" |
| Pinned | Blue ⚑ pin icon + 6px indigo dot top-right corner |

### Delete action
- Available in hover footer (🗑 button, turns red on hover)
- Available in right-click context menu
- Shows a brief inline confirmation ("Delete?  Yes / No") **within the card footer** before removing — no modal

### Right-click context menu
- Paste
- Copy only
- Pin / Unpin
- Delete (with inline confirm)

---

## 5. App Accent Colours

The `SourceIdentity` model will gain an `AccentColor` property. A built-in lookup table maps well-known process names to colours; unknown apps get a colour derived by hashing their process name.

| App | Accent hex |
|-----|-----------|
| VS Code (`code`) | `#0078D4` |
| Figma (`figma`) | `#A259FF` |
| Chrome (`chrome`) | `#4285F4` |
| Slack (`slack`) | `#4A154B` |
| Claude / claude.exe | `#D4885E` |
| Terminal / PowerShell | `#22C55E` |
| Notion (`notion`) | `#191919` |
| Word (`winword`) | `#2B579A` |
| Excel (`excel`) | `#217346` |
| Unknown | Hash of process name → HSL colour |

---

## 6. Theme System

### Three modes (stored in `AppSettings.Theme`)
- **`System`** — follows `SystemParameters` / `SystemTheme` (Windows 10/11 dark mode API)
- **`Dark`** — always dark
- **`Light`** — always light

### Theme tokens
Two `ResourceDictionary` files will hold all colour tokens:
- `Themes/Dark.xaml`
- `Themes/Light.xaml`

Applied at runtime by merging the correct dictionary into `Application.Resources`. Theme change is **instant** (no animation required).

### Key tokens (examples)
Values below are design intent. Implementation uses WPF `#AARRGGBB` hex (e.g. `#E80A0A0E`) or `Color` with `A` channel.

| Token | Dark | Light |
|-------|------|-------|
| `PanelBackground` | `#0A0A0E` @ 91% alpha | `#F8F9FC` @ 97% alpha |
| `CardBackground` | `#FFFFFF` @ 4% alpha | `#FFFFFF` |
| `CardBorder` | `#FFFFFF` @ 6% alpha | `#000000` @ 7% alpha |
| `BodyText` | `#A1A1AA` | `#374151` |
| `MetaText` | `#3F3F46` | `#9CA3AF` |
| `SearchBackground` | `#FFFFFF` @ 6% alpha | `#000000` @ 5% alpha |
| `ChipIdle` | `#FFFFFF` @ 6% alpha | `#000000` @ 6% alpha |
| `FooterBorder` | `#FFFFFF` @ 5% alpha | `#000000` @ 6% alpha |
| `AccentPrimary` | `#6366F1` | `#4F46E5` |

### Settings UI addition
New "Appearance" section added to `SettingsWindow` above the existing sections:

```
APPEARANCE
┌────────────────────────────────────────┐
│ Theme   [System ▾]  (dropdown: System / Dark / Light)  │
└────────────────────────────────────────┘
```

`SettingsWindow` itself also respects the current theme.

---

## 7. Keyboard Navigation

| Key | Action |
|-----|--------|
| `→` | Move selection to next card (scroll viewport to keep in view) |
| `←` | Move selection to previous card |
| `Enter` | Paste selected card content, then dismiss panel |
| `Ctrl+Enter` | Copy selected card to clipboard only (panel stays open) |
| `P` | Toggle pin on selected card |
| `Del` | Show inline delete confirm on selected card |
| `Ctrl+F` | Focus search box |
| `Esc` | Dismiss panel (no action) |
| `Tab` | Move focus to search / filter chips |

**Focus-follows-selection scroll:** When the selected card is partially or fully outside the visible scroll viewport, the `ScrollViewer` animates to bring the card fully into view (smooth scroll, ~150 ms).

**Boundary behaviour:** Selection stops at the first card (pressing `←`) and at the last card (pressing `→`). It does not wrap around.

---

## 8. Search & Filtering

Behaviour unchanged from the current implementation. The search box and filter chips remain in the header. When a search is active, `ResultCount` label appears to the right of the filter chips (e.g. "12 results").

**Empty states:**
- No history: clipboard icon + "Copy something to see it here"
- No search results: magnifier icon + "No matching clips"
- Both are centred in the card row area

---

## 9. Banners (Paused / Error)

The amber "capture paused" banner and red error banner appear as a thin strip (32px) **between the header and the card row**. They push the card row down when visible; the panel does not grow taller — the card row loses 32px of height when a banner is shown. Only one banner can be visible at a time (error takes priority over paused).

---

## 10. Files Affected

| File | Change |
|------|--------|
| `Views/PopupWindow.xaml` + `.cs` | Full rewrite |
| `Controls/ClipCard.xaml` + `.cs` | Full rewrite |
| `Controls/SourceBadge.xaml` + `.cs` | Minor — expose AccentColor |
| `Views/SettingsWindow.xaml` + `.cs` | Add Appearance section, apply theme |
| `Models/SourceIdentity.cs` | Add `AccentColor` property |
| `Themes/Dark.xaml` *(new)* | Dark resource dictionary |
| `Themes/Light.xaml` *(new)* | Light resource dictionary |
| `AppSettings.cs` | Add `Theme` enum property |
| `App.xaml.cs` | Theme bootstrap + `SystemTheme` listener |
| `ViewModels/PopupViewModel.cs` | Add `SelectedCardIndex`, scroll-into-view logic |

---

## 11. Out of Scope (V1)

- Animations between theme switches
- Pasteboard / collection grouping (future feature)
- Drag-to-reorder cards
- Customisable card width or panel height
- Taskbar position detection (panel always overlaps taskbar)
