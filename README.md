# MultiBox 🛡️

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue.svg)](https://microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-x64-lightgrey.svg)]()
[![Latest Release](https://img.shields.io/github/v/release/uranrog-01/MultiBox?color=green)](https://github.com/uranrog-01/MultiBox/releases/latest)
[![Author](https://img.shields.io/badge/Author-zer0ne-success.svg)](https://github.com/uranrog-01)

> **Run the same Windows app multiple times concurrently — completely isolated, with zero shared logins, cookies, or data, as if freshly installed on a brand-new PC.**

---

## 💡 What is MultiBox?

Most modern desktop apps — including **Discord, Telegram, Slack, Chrome, WhatsApp, Edge, and VS Code** — prevent you from opening multiple copies at once, or force all open windows to share the exact same user account, cookies, and cache.

**MultiBox solves this effortlessly.**

With MultiBox, you can open as many copies of the same program as you need. Every single copy runs inside its own **independent sandbox**:
- ❌ **No shared accounts**: Log into 5 different Discord, Telegram, or Google accounts at the same time.
- ❌ **No shared cookies or storage**: Each copy behaves as if it was just downloaded and freshly installed on a clean computer.
- ❌ **Zero traces left behind**: When you close and wipe a session, all temporary data and caches vanish completely.
- ⚡ **Zero app restarts**: Switch between your active sessions smoothly from a single, modern flyout dashboard.

---

## 📸 Visual Tour & How It Works

### 1. The Dashboard & Session Capsules
*Launch duplicate copies in seconds and switch between them seamlessly.*

<p align="center">
  <img src="docs/screenshots/dashboard.png" alt="MultiBox Dashboard" width="700" style="border-radius: 12px; box-shadow: 0 8px 30px rgba(0,0,0,0.5);" />
</p>

- **Drag & Drop**: Simply drop any application executable (`.exe`) or desktop shortcut onto MultiBox to launch your first isolated copy.
- **Another Copy**: Click **Another copy** to immediately spawn a duplicate, freshly-isolated instance of that app.
- **Session Capsules**: Click capsules (*Chrome #1*, *Chrome #2*, *Discord #1*, etc.) to switch telemetry, view session health, or bring that specific app window to the front.
- **Custom Labels & Colors**: Double-click any capsule to give it a custom name (e.g. *Work*, *Personal*, *Client A*) and pick from 6 accent colors.

---

### 2. The Vault
*Your private, isolated session sandbox.*

<p align="center">
  <img src="docs/screenshots/vault.png" alt="MultiBox Vault" width="700" style="border-radius: 12px; box-shadow: 0 8px 30px rgba(0,0,0,0.5);" />
</p>

- **How the Vault Works**: When an app opens through MultiBox, all of its settings, login tokens, downloaded files, and local databases are redirected into a private, temporary folder (its **Vault**).
- **Total Isolation**: App Instance #1 can never see, modify, or read data from App Instance #2.
- **1-Click Wipe**: Done with a temporary session? Click **End & wipe** to close that app and immediately erase its temporary vault files. Your main PC stays 100% clean.

---

### 3. SaveBox Profile Library
*Save your favorite setups and restore them anytime.*

<p align="center">
  <img src="docs/screenshots/savebox.png" alt="SaveBox Profile Library" width="700" style="border-radius: 12px; box-shadow: 0 8px 30px rgba(0,0,0,0.5);" />
</p>

- **How SaveBox Works**: While temporary sessions wipe when closed, you often want to keep specific accounts permanent — such as a permanent *Work Discord* or *Trading Browser Profile*.
- **Save Profiles**: In **Vault → Apps**, click **Save** on any running session. Give it a name (e.g., *Client Work*, *Crypto Portfolio*), and SaveBox securely snapshots its databases and settings.
- **1-Click Restore**: Go to **Vault → Saved** and click any saved profile. MultiBox launches it as a brand-new independent instance instantly!

---

### 4. Tasks & Live Container Telemetry
*Monitor running processes and isolate active programs.*

<p align="center">
  <img src="docs/screenshots/tasks.png" alt="MultiBox Tasks Panel" width="700" style="border-radius: 12px; box-shadow: 0 8px 30px rgba(0,0,0,0.5);" />
</p>

- **Running Process Monitor**: Inspect memory, CPU usage, and process status for all isolated instances.
- **Isolate Running Apps**: See an app already running on your PC? Click it in Tasks to capture and isolate it immediately.
- **Telemetry Reactor HUD**: Dynamic animated particle reactor and activity frequency bars reflect your active container's live telemetry.

---

### 5. System Tray Flyout & Smooth Motion
*Docks neatly next to your clock — out of the way until you need it.*

- **Minimize to Tray**: Click the minimize (`_`) or close (`X`) button in the titlebar. MultiBox slides smoothly down into the Windows notification tray, freeing your screen and taskbar.
- **Fluid Slide-In Motion**: Click the MultiBox icon in the system tray or launch `MultiBox.exe` again: the window glides smoothly up from the tray with a hardware-composited cubic ease-out motion effect.
- **Quick Tray Menu**: Right-click the tray icon for quick actions: reopen, view status, wipe sessions, or exit.

---

## 🚀 Quick Start Guide

### Step 1: Download
Download the latest pre-packaged release:
👉 [**Download `MultiBox-v1.0.3-win-x64.zip`**](https://github.com/uranrog-01/MultiBox/releases/latest)

### Step 2: Extract & Run
Extract the `.zip` archive to any folder on your PC and double-click **`MultiBox.exe`**.

### Step 3: Start Multitasking!
Drag and drop any application shortcut or `.exe` onto MultiBox and enjoy complete multi-instance freedom.

---

## 🔒 SmartScreen & Publisher Trust

MultiBox is digitally signed with an Authenticode certificate issued to **`zer0ne`**.

When downloading executables from GitHub, Windows SmartScreen may show an *"Unknown Publisher"* prompt on the very first run:
1. Click **"More info" → "Run anyway"**.
2. *Optional (Permanent 1-Click Trust)*: Open MultiBox Settings (⚙️) and click **"Trust Publisher on this PC"** — this permanently configures Windows to trust MultiBox with zero future prompts.

---

## 🛠️ Building from Source

If you prefer building MultiBox yourself:

```powershell
# 1. Clone the repository
git clone https://github.com/uranrog-01/MultiBox.git
cd MultiBox

# 2. Build the solution (.NET 10 SDK required)
dotnet build -c Release

# 3. Publish single-file standalone executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

To run the native automated integration test suite (26 checks):
```powershell
dotnet build tests/SessionFixture -c Release
dotnet run --project tests/SessionTests -c Release -- tests/SessionFixture/bin/Release/net10.0-windows
```

---

## 🔬 Deep Technical Architecture

MultiBox achieves user-mode isolation using low-level **Windows Job Objects**, **SQLite Write-Ahead Logging (WAL) recovery**, dynamic environment redirection, and Desktop Window Manager (DWM) composition.

For a full breakdown of the internals, process containment models, and database snapshot protocols, read our dedicated technical guide:

👉 [**Read the Architecture & Technical Internals Guide (`ARCHITECTURE.md`)**](ARCHITECTURE.md)

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

Copyright © 2026 **zer0ne** ([https://github.com/uranrog-01](https://github.com/uranrog-01))
