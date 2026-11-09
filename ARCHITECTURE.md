# MultiBox Architecture

> **Author**: [zer0ne](https://github.com/uranrog-01) · **Platform**: Windows 10 / 11 (x64) · **Runtime**: .NET 10, single-file

How MultiBox starts apps in separate sessions, keeps their data apart, saves profiles and handles apps that normally refuse a second copy. For the user guide, see the [README](README.md).

## Contents

1. [Overview](#overview)
2. [Process containment: Job Objects](#1-process-containment-job-objects)
3. [Storage redirection: the Vault](#2-storage-redirection-the-vault)
4. [App compatibility layer](#3-app-compatibility-layer)
5. [SaveBox: saving and restoring profiles](#4-savebox-saving-and-restoring-profiles)
6. [User interface](#5-user-interface)
7. [Security notes](#6-security-notes)
8. [Limitations](#7-limitations)
9. [Distribution and setup](#8-distribution-and-setup)

---

## Overview

MultiBox runs in user mode. It needs no administrator rights, kernel drivers or hypervisor.

```mermaid
flowchart TD
    Launcher[MultiBox] -->|CreateProcess, suspended| Job[Job Object per session]
    Launcher -->|Environment block| Vault[Vault folder per session]
    Launcher -->|Before launch| Compat[Compatibility layer]

    subgraph JobBoundary["Job Object"]
        App["App (e.g. Discord.exe)"]
        Child1["GPU / renderer processes"]
        Child2["Background services (Node.js, Bun)"]
    end

    Job --> App
    App --> Child1
    App --> Child2

    subgraph StorageBoundary["%LOCALAPPDATA%\MultiBox\Vaults\<id>\User"]
        Roaming["AppData\Roaming"]
        Local["AppData\Local"]
        Temp["AppData\Local\Temp"]
        Home[".config / .local / .cache"]
    end

    App --> StorageBoundary
    Child2 --> StorageBoundary

    StorageBoundary -.->|Save| SaveBox["Vaults\SaveBox\<profile id>"]
```

---

## 1. Process containment: Job Objects

Every session owns one Windows Job Object (`Core/JobObject.cs`).

- **Created suspended**: the app is started with `CREATE_SUSPENDED`, assigned to the job, then resumed. Even very fast child processes therefore start inside the job.
- **Child processes**: renderers, GPU helpers, crash reporters and detached Node.js/Bun services inherit the job automatically.
- **Process list**: `QueryInformationJobObject(JobObjectBasicProcessIdList)` returns the session's exact process IDs. Window focus, telemetry and closing use this list, never executable names, so two copies of the same `.exe` never mix.
- **Cleanup**: the job is created with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, so closing a session ends its whole process tree.

---

## 2. Storage redirection: the Vault

Each session gets a private folder: `%LOCALAPPDATA%\MultiBox\Vaults\<12-character id>\`.

### Environment variables

| Variable | Points to |
| :--- | :--- |
| `USERPROFILE`, `HOME`, `HOMEDRIVE` + `HOMEPATH` | `<Vault>\User` |
| `APPDATA` | `<Vault>\User\AppData\Roaming` |
| `LOCALAPPDATA` | `<Vault>\User\AppData\Local` |
| `TEMP`, `TMP` | `<Vault>\User\AppData\Local\Temp` |
| `XDG_CONFIG_HOME`, `CHROME_CONFIG_HOME` | `<Vault>\User\.config` |
| `XDG_DATA_HOME` | `<Vault>\User\AppData\Local` |
| `XDG_STATE_HOME`, `XDG_CACHE_HOME` | `<Vault>\User\.local\state`, `<Vault>\User\.cache` |

Windows resolves the *Roaming AppData*, *Local AppData*, *Documents*, *Desktop* and *Downloads* known folders from these variables, in both 64-bit and 32-bit (WOW64) processes. This was verified with `SHGetKnownFolderPath`, Node.js `os.homedir()`/`os.tmpdir()` and Bun.

### Profile arguments

| Engine | Detected by | Argument added |
| :--- | :--- | :--- |
| Chromium / Electron | `resources\app.asar`, `chrome.dll`, `ffmpeg.dll`, or the exe name | `--user-data-dir="<Vault>\User\AppData\Roaming\<ExeName>" --no-first-run --no-default-browser-check` |
| Firefox / Thunderbird | exe name | `-profile "<Vault>\User\AppData\Local\UserData" -no-remote` |

Electron apps that choose their own folder with `app.setPath('userData')` (for example OpenCode's `ai.opencode.desktop`) still land inside the Vault, because Electron derives `appData` from the redirected known folder.

### Safe deletion

`Core/VaultRecovery.cs` checks NTFS reparse points before deleting. Junctions and symbolic links are unlinked, never followed, so a wipe can't delete folders outside the Vault.

---

## 3. App compatibility layer

Some apps collide with their first copy through resources that environment variables can't redirect. `Core/AppCompatibility.cs` runs before each launch:

| Problem | Affected apps | Fix |
| :--- | :--- | :--- |
| **Named single-instance mutex.** A second copy finds the mutex, sends its arguments to the open window and exits. | Tauri apps (`<identifier>-sim`, e.g. Cline), apps using `single-instance` mutex names, and launchers with a fixed well-known mutex (Half-Life's `ValveHalfLifeLauncherMutex`) | `SingleInstanceLocks` finds other running copies of the **same executable path**, reads their mutex names (`NtQuerySystemInformation` + `NtQueryObject`) and closes only names that match single-instance conventions or the known-name list (`DuplicateHandle` with `DUPLICATE_CLOSE_SOURCE`). Other mutexes are not touched. Works for 32-bit and 64-bit targets. |
| **Fixed localhost service port.** | OpenCode Desktop (`opencode-cli serve --service`, port 45278) | Writes a free per-session port to `<Vault>\User\.config\opencode\service.json` (the file `opencode service set port` uses) and removes the stale service registration from restored profiles. |
| | Cline (hub daemon, port 25463) | Sets `CLINE_HUB_PORT` to a free per-session port. Without it, the copy would attach to, or fail against, the host's hub. |
| **Shared WebView2 data folder.** | WebView2 apps that never set a data folder (`<exe>.WebView2` next to the exe) | Sets `WEBVIEW2_USER_DATA_FOLDER` into the Vault. Apps that choose their own folder (Tauri's `EBWebView`) already follow the Vault and are left alone. |
| **Packaged apps and Codex folder browsing.** | Microsoft Store desktop apps (`WindowsApps`), e.g. Codex, Claude | Started by path like any other app. If Windows denies the start, MultiBox explains that instead of showing "Access is denied". Codex/ChatGPT also receive the real `USERPROFILE`/`HOME`, because Electron's Open Folder dialog starts in Downloads (falling back to the profile) and Codex's exec host reads `USERPROFILE`. `APPDATA`, `LOCALAPPDATA`, `TEMP`, `CODEX_HOME` and `--user-data-dir` stay in the Vault, so sign-in, session and browser state remain per session. |

Ports are chosen from a fixed range, stay stable per session while free, and are never handed to two sessions at once.

### App categories: the two axes

Every app MultiBox meets is classified on two independent axes, and each combination decides what launch support it needs. A runtime tells MultiBox **where the app keeps its data** (the Vault strategy); a single-instance mechanism tells MultiBox **why a second copy refuses to start** (the compatibility strategy). New app support is always one of three moves: extend a table, add a handler in `AppCompatibility.Prepare`, or document the case as unsupported.

**Axis 1 — runtime (data isolation):** covered by the [runtime summary](#runtime-summary) below — Electron/Chromium, Firefox/Gecko, WebView2, Tauri, Node.js/Bun, .NET (Framework and modern; `%APPDATA%` redirection works the same as Win32), classic Win32/Win64, Microsoft Store packages.

**Axis 2 — single-instance mechanism:**

| Mechanism | How the app does it | MultiBox handling | Status |
| :--- | :--- | :--- | :--- |
| None | Runs any number of copies | Nothing needed | ✅ default |
| Profile lock | Chromium/Electron `SingletonLock`/`lockfile` inside the user-data dir | Own user-data dir per Vault | ✅ |
| Engine flags | Firefox `-profile … -no-remote`, Electron `--user-data-dir` | Arguments injected at launch | ✅ |
| Mutex by convention | Mutex name contains `single-instance`/`singleinstance` | Mutex closed in other copies and in the session's own copy | ✅ |
| Known fixed mutex | A hard-coded name following no convention — Half-Life's `ValveHalfLifeLauncherMutex` | Known-name table in `SingleInstanceLocks`; extend per verified app | ✅ extendable |
| Tauri window pair | `<id>-sim` mutex + `<id>-sic`/`<id>-siw` forwarding window | Window renamed (not the mutex), launch passes, then the session's own mutex is closed | ✅ |
| Fixed service port | Binds a localhost port at startup (OpenCode 45278, Cline hub 25463) | Per-session ports; per-app env/config fixes | ✅ per app |
| Storefront dependency | Launcher exits without its storefront client — `hl.exe` relays through Steam and exits, and Steam spawns the real game outside the session's job | `AppCompatibility.EnsureStorefrontRunning` starts Steam silently when missing (the relay the launcher itself performs); the engine layer passes `-steam`, the argument Steam itself passes, so the launcher runs its engine inside the session's job | ✅ |
| Window-class check | `FindWindow(class)` finds the first copy and forwards/exits (common in older Win32 apps and games) | Not covered yet | 🔜 future |
| Named-pipe handoff | First copy runs a pipe/socket server; the second forwards its command line and exits (Qt `QLocalServer`, VB.NET `WindowsFormsApplicationBase`) | Not covered yet | 🔜 future |
| Pipe-brokered handoff | First copy hosts a fixed, machine-global named pipe (Malwarebytes: managed `MbamUI.Services.SingleInstanceService` on `\Device\NamedPipe\MbamUI_B1CDD42D-DA9D-4063-A195-76AF6FED2554_<sid>` plus a matching semaphore; the name is built in `GenerateSemaphoreName` from a **hard-coded GUID literal + `WindowsIdentity.GetCurrent().User`** — verified by IL decompilation, no other input exists); the second copy connects as a client, forwards its arguments and exits while the open window activates | Same account: refused before launch with the explanation — the name is a constant plus the process account's SID, so no profile, environment or registry state can vary it. **"Different user…"** in the refusal toast launches the second copy under another Windows account (`CreateProcessWithLogonW`, vault environment kept, session job kept): the app's pipe name embeds the user SID, so the two copies compute different names and run side by side | ✅ via different Windows account |
| Named-event master handoff | First copy holds a machine-global named event that the second copy checks before forwarding its launch to it (Steam's bootstrap client: `Global\Valve_SteamIPC_Class` — verified in `steam.exe` as a string literal used with `OpenEventA`/`CreateEventA`, and by enumerating the running client's named handles; the event's DACL is **NULL**, so every Windows account sees it, and a bare second launch exits with code 0 in ~60 ms while the open window activates). The command-line switch `-master_ipc_name_override` (a literal in the same binary) replaces the master IPC name: a copy launched with it becomes the master of its own `<override>_SharedMemFile`/`<override>_SharedMemLock` instead of forwarding — verified live, the second copy survives alongside the first. One lock stays out of reach: the browser UI (`steamwebhelper`) receives `-cachedir=<real profile>\AppData\Local\Steam\htmlcache` — a known folder the Vault's environment cannot redirect, verified by launching with a redirected `LOCALAPPDATA` and watching the real path still being passed — and Chromium's cache lock lets only one webhelper initialize per account | Same account: refused before launch — even a first vaulted client would host that fixed browser cache and take it away from the user's own Steam, and any second client on the account is left "Steamwebhelper is not responding". **"Different user…"** launches the copy under another Windows account (`CreateProcessWithLogonW`, vault environment kept, session job kept) and MultiBox appends `-master_ipc_name_override=<vault id>` (`AppCompatibility.MasterIpcOverrideArgument`), so the copy is a real master with its own cache, its own `HKCU\Software\Valve\Steam` state and its own sign-in | ✅ via different Windows account |
| Self-protected / anti-cheat / DRM | A driver blocks handle access (Malwarebytes self-protection, EAC/BattlEye, Denuvo) | Not attempted — touching these is both impossible and wrong | ❌ never |

Two design rules keep the mechanism axis safe: mutexes are only ever closed in processes running the **identical executable path**, and generic name matching stays deliberately conservative (a mutex named after the app, like many .NET/WPF apps use, is *not* matched — closing an unknown mutex can break an unrelated feature). Anything that doesn't fit a safe pattern goes on the known-name table only after being verified in the app's binary, the way `ValveHalfLifeLauncherMutex` was.

### Runtime summary

| Runtime | Profile data | Second copy |
| :--- | :--- | :--- |
| Node.js | `os.homedir()`, `os.tmpdir()`, `%APPDATA%` → Vault | ✅ |
| Bun (incl. single-file executables) | Same as Node.js; spawned children inherit the Vault | ✅ |
| Electron | `userData`, single-instance lock keyed on it → Vault | ✅ |
| Tauri | Data via known folders → Vault; mutex released | ✅ |
| WebView2 | App-chosen folder → Vault; default folder redirected | ✅ |
| .NET (Framework and modern) | `%APPDATA%`/`%LOCALAPPDATA%` env vars → Vault; registry-backed settings shared | ⚠️ |
| Win32 / WOW64 | Known folders → Vault; registry shared | ⚠️ |

Codex/ChatGPT is the one deliberate exception to the vaulted home: its account, session and browser data stay in the Vault, but the profile root is real so **Open Folder** and shell tools see the machine's actual user folders instead of an empty fake profile. Files the app writes directly to the real profile (for example `~/.cache`) are shared with the host and are not part of SaveBox.

---

## 4. SaveBox: saving and restoring profiles

A running app holds locks on its databases, so SaveBox uses a **close, save, reopen** cycle (`IsolationEngine.SaveProfileAndReopen`):

1. **Close**: `CloseMainWindow` is sent to every process in the job. If a window stays open (for example an unsaved-work prompt), the save stops and nothing is changed. Remaining background processes in the session's job are then ended.
2. **Copy** (`Core/LiveProfileSnapshot.cs`), into a staging folder:
   - SQLite databases are copied with the **online backup API** and saved as standalone files (`journal_mode=DELETE`), never as a database plus a stray WAL.
   - LevelDB folders are checked before and after copying; a change during the copy causes a retry.
   - Skipped: lock and marker files (`SingletonLock`, `lockfile`, `parent.lock`, …), regenerated stores (`DIPS`, `declarative_performance_observer.db`) and **cache folders** (`Cache`, `Code Cache`, `GPUCache`, `DawnCache`, …). Caches are recognized by layout inside any Chromium/Electron/WebView2 profile, not only the exe-named one.
   - Generated runtimes are skipped too, such as OpenCode's ~200 MB staged CLI under `ai.opencode.desktop\cli`, which the app restores itself.
3. **Commit**: the staging folder replaces the profile atomically; the old copy is kept as a backup until the move succeeds.
4. **Reopen**: the app restarts in the same session.

**Restore** copies the profile into a fresh Vault, rewrites stored Vault paths in text files and SQLite databases (`Core/PathMigrationHelper.cs`), and removes files Chromium must rebuild for a new path (`Secure Preferences`, `Web Data`, singleton locks). A profile opened from SaveBox stays **linked**: when its session closes, changes sync back with the same exclusions.

Each profile folder holds `Data\` and a `manifest.json` with the app path, save time, size and original Vault path.

---

## 5. User interface

- **WebView2 + Windows Forms**: the UI is local HTML in a frameless window with DWM rounded corners (`DWMWA_WINDOW_CORNER_PREFERENCE`).
- **Message bridge**: `chrome.webview.postMessage` messages are accepted only from the packaged UI page. Other navigations and new windows are blocked.
- **Tray motion**: `timeBeginPeriod(1)` enables a high-rate timer; the window slides with a cubic ease-out (`1 - (1 - t)^3`, about 240 ms) and is hidden from the taskbar when docked.
- **Window marking**: a colored border and label follow the selected session's windows.

---

## 6. Security notes

- **Mark-of-the-Web**: on start, MultiBox removes the `Zone.Identifier` stream from its own files (`DeleteFileW(path + ":Zone.Identifier")`) so extracted DLLs load without prompts.
- **Code signing**: releases are signed with a self-signed certificate (`CN=zer0ne`). Windows treats it as an unknown publisher, so SmartScreen warns on first run.
- **Handle closing** (section 3) is limited to processes with the identical executable path, running as the same user, and to mutex names matching single-instance patterns.

---

## 7. Limitations

MultiBox separates **profiles**. It is not a security boundary.

- **Registry**: `HKEY_CURRENT_USER` is shared. Apps that keep settings or accounts in the registry share them between copies.
- **Credential Manager / DPAPI vault**: shared per Windows user. Apps that keep tokens there (for example via `keytar`) may share a login.
- **Folders not driven by environment variables**: `AppData\LocalLow` and the *Profile* known folder (Electron `app.getPath('home')`, Rust `dirs::home_dir`) point to your real profile. Codex/ChatGPT use the real profile root on purpose for folder browsing; their account/session state is still vaulted.
- **File and network access** are not restricted.
- **Other single-instance methods**, such as named pipes, fixed ports in other apps or window-class checks, are handled only for the apps listed in section 3.
- **Pipe-brokered single instance** (Malwarebytes) cannot be bypassed at all, and there is nothing to bypass: the second `Malwarebytes.exe` copy holds no single-instance mutex — it connects as a **client** to the first copy's fixed, machine-global named pipe, forwards its arguments, and exits with code 0 while the first window activates. The pipe name is built by `MbamUI.Services.SingleInstanceService.GenerateSemaphoreName` as `"MbamUI_" + new Guid("B1CDD42D-DA9D-4063-A195-76AF6FED2554") + "_" + WindowsIdentity.GetCurrent().User` — a hard-coded GUID plus the process account's SID, with no other input (verified by IL decompilation of every method involved, and by a fresh-vault session producing the byte-identical name). Two UIs cannot share one pipe name; because the only variable is the account SID, the one legitimate escape is a **different Windows account**. MultiBox now refuses such launches up front with that explanation instead of letting the copy flash and close.
- **Steam** has two locks, and only the account separates both. First, the handoff: `steam.exe` checks the machine-global event `Global\Valve_SteamIPC_Class` (string literal in the binary, used with `OpenEventA`/`CreateEventA`; NULL DACL — every account sees it; verified against the running client's handle table). A second bare launch exits with code 0 in ~60 ms; with `-master_ipc_name_override=<name>` — a literal in the same binary — the copy instead becomes the master of `<name>_SharedMemFile`/`<name>_SharedMemLock` and runs (verified live: two `steam.exe` processes side by side). Second, the browser UI: `steam.exe` spawns `steamwebhelper` with `-cachedir=<real profile>\AppData\Local\Steam\htmlcache`. That path comes from the process token's known folder, not from the environment — a launch with a redirected `LOCALAPPDATA`, and even one with a `-cachedir` argument, still produced the real path (no `htmlcache`/`-cachedir` literal exists in `steam.exe`; the path is computed from the Windows account) — and Chromium's cache lock (`htmlcache\lockfile`) lets only one webhelper initialize per Windows account: the second client stays "Steamwebhelper is not responding" (its webhelper tree spawns 1 child instead of the healthy 5–7). A vaulted first client would even take that cache away from the user's own Steam. So **no profile or Vault folder can separate Steam**: the cache follows the Windows account. MultiBox refuses same-account `steam.exe` launches up front, and its different-account launch appends `-master_ipc_name_override=<vault id>`; on the other account the cache, `HKCU\Software\Valve\Steam` (auto sign-in) and the session's handoff objects all separate naturally — verified live with a temporary standard account: its client received `C:\Users\<account>\AppData\Local\Steam\htmlcache` and ran a full webhelper tree beside the original, then the account was deleted. One Steam window per Windows account, like Malwarebytes — but here the different-account copy additionally needs the override, because the NULL-DACL event would otherwise hand its launch to the original client in the same session.
- **Storefront relays** (Half-Life via Steam): the GoldSrc launcher either runs its engine in-process (`-steam`, how Steam launches it) or hands off to the Steam client and exits. MultiBox passes `-steam` and starts the Steam client when missing, so the game runs inside the session's job. That relay starts Steam in the user's own client context (no override, no vault) — exactly what the launcher itself would do. The Steam client itself allows only one running instance per AppID, which no user-mode launcher can override.

---

## 8. Distribution and setup

Two artifacts ship from the same source: a portable zip (`build-release.ps1`) and a setup wizard (`build-setup.ps1`).

- **Embedded payload**: `build-setup.ps1` publishes the app, packs it into `setup/payload.zip`, and writes `setup/payload.json` (version, sizes, file list). Both are embedded as resources into `MultiBox.Setup.exe`, so the installer is one file with nothing to download alongside it. `MultiBox.csproj` excludes `setup/**` so the wizard's sources never compile into the app.
- **Setup project** (`setup/`, WPF, no third-party UI packages): a four-step wizard — welcome, destination, installing, finish — sharing the app's dark palette (`#050807` with lime `#BAFF2A`). `InstallEngine` streams the archive entry by entry, reports real byte progress, writes an on-disk `manifest.json`, creates shortcuts through `WScript.Shell`, and registers the Add/Remove Programs key.
- **Rollback**: cancelling or failing mid-install deletes the files written so far and removes the destination folder if the installer created it.
- **Upgrade detection**: `InstallTargets.Detect()` checks the registry key, then the default folders, and the welcome page reports an in-place upgrade.
- **Headless modes**: `--silent`, `--install-to`, `--all-users`, `--extract-to`, `--version`, `--help` run without a window and return meaningful exit codes, which is also how CI smoke-tests the artifact. `--extract-to` is a pure portable unpack that writes no shortcuts and no registry keys.
- **Uninstall**: the app itself hosts the uninstaller (`Uninstall.exe`, `MultiBox.exe --uninstall`, `UninstallForm.cs`) so no second executable ships. The setup creates `Uninstall.exe` as a **hard link** to `MultiBox.exe` — a separately launchable uninstaller with zero extra disk space — and falls back to `MultiBox.exe --uninstall` on file systems without hard links. It closes running app copies, removes shortcuts and the registry key, asks separately about session data, and defers the folder delete to a retrying script that runs after the process exits — the running `.exe` locks its own folder.
