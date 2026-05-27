# Zapret UI — User Guide

> Online: https://raccoonlaptop.github.io/ZapretUI/

## Installation

1. Download **ZapretUI-Setup.exe** from [Releases](https://github.com/RaccoonLaptop/ZapretUI/releases).
2. Run the installer and confirm UAC if prompted.
3. On first launch the app downloads Flowseal components (one-time, internet required).

Install path: `%LocalAppData%\ZapretUI`. Zapret configs: `%LocalAppData%\ZapretUI\zapret`.

## Home — start bypass

1. Open **Home**.
2. Pick a strategy (e.g. `general.bat`).
3. Press **START**.
4. Status shows **Running** (green dot).
5. Press **STOP** to end bypass.

Use the ✦ button to switch animated backgrounds (default: **Wavy**).

## Strategies

- View and edit `.bat` files with syntax highlighting.
- **Run** starts the selected strategy.
- Copy, rename, delete (protected: `service*.bat`).
- **Lists** — edit `lists/*.txt` for the config.
- Find & replace in the editor.

## Service

| Section | Purpose |
|---------|---------|
| Game Filter | Game ports: TCP+UDP / TCP only / UDP only / off |
| IPSet Filter | Download IP list from Flowseal |
| Auto-update | Check for updates on startup |
| Language | Russian / English (restart required) |
| Security | Windows Defender exclusions and firewall rules |
| Updates | Zapret UI and Flowseal |
| Network reset | `netsh` + DNS flush (admin) |

Fresh install defaults: Game Filter **TCP+UDP**, IPSet **loaded**.

## Console

Log window for PowerScript and app messages. Open from **Console** in the sidebar.

## System tray

The Zapret UI icon stays in the notification area.

- **Right-click** — menu: status, start/stop, open app, exit.
- Green ring on the icon when bypass is **running**.
- **Double-click** — open the main window.

Closing the window with bypass running minimizes to tray (bypass keeps running).

## Updates

When a new version is available you’ll be prompted. The app downloads `ZapretUI-Setup.exe`, installs with progress, then restarts.

## Requirements

- Windows 10/11 x64
- Administrator rights (requested automatically)
- Internet for first Flowseal download and updates

## Troubleshooting

| Issue | Action |
|-------|--------|
| “winws did not start” | Run as administrator; try another strategy |
| Error after stop | Update to v1.6.4+ |
| App won’t open | Install latest release from GitHub |
| Sites still blocked | Try another strategy; update Flowseal in Service |

## Credits

- [bol-van/zapret](https://github.com/bol-van/zapret)
- [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube)
