# Changelog

All notable changes to MultiBox. Versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- **Two windows side by side for pipe-brokered apps (Malwarebytes) via a different Windows account.** The app's single-instance pipe name embeds the user SID, so a second copy launched under another account computes a different pipe name and both windows run at once — the same side-by-side experience as every other app. When the same-account launch is refused, the toast now offers **"Different user…"**: enter the account name and password, and MultiBox starts the second copy with `CreateProcessWithLogonW` — still inside the session job, still with the vaulted environment, so its profile data stays in the Vault too. Reopen keeps the account. (Blank-password accounts are rejected by Windows policy; use an account with a password.)
- **MultiBox Setup** — a modern setup wizard (`MultiBox-Setup-v<version>-win-x64.exe`) for people who want a normal Windows install instead of a zip. A single self-contained file with the whole app embedded: browse for a destination, choose shortcuts, watch real progress, and finish with **Launch MultiBox now**. It matches the app's own dark, luminous design.
  - Installs per user (no admin prompt, the default) or for all users into `Program Files`, and registers a proper entry in **Settings → Apps → Installed apps**.
  - Running a newer setup over an existing install upgrades in place and keeps your sessions; an existing install is detected and shown on the welcome page.
  - Unattended and portable modes: `--silent`, `--install-to`, `--all-users`, `--no-desktop-shortcut`, `--no-start-menu`, `--extract-to`, `--version`, `--help`.
  - Cancelling mid-install rolls back the files it had written.
  - `build.bat` at the repository root builds the installer for whatever version is in `MultiBox.csproj` — bump the version, run it, and `dist\` gets the new setup. `build.bat quick` skips the app publish, `build.bat zip` also builds the portable zip.
- **Uninstaller** — a real `Uninstall.exe` in the install folder (installed as a hard link to the app, so it costs no extra space), also reachable from **Settings → Apps**, the Start menu folder, and `MultiBox.exe --uninstall`. It closes running MultiBox copies, removes the shortcuts and registry entry, and asks separately before deleting your sessions and profiles. The install folder is cleaned up after the app exits.

### Fixed
- **Malwarebytes now explains itself instead of flashing and closing.** Reverse engineering (managed metadata, live handle tables and the app's own log) showed the second `Malwarebytes.exe` copy is not blocked by any mutex: its managed `SingleInstanceService` connects to the first copy's fixed machine-global named pipe (`MbamUI_<guid>_<user-sid>`), forwards its arguments — the first instance logs "Received arguments from second instance. First instance will display MainWindow." — and exits. Two UIs cannot share one pipe name, so one window per Windows account is the app's design; the one legitimate escape is a different Windows account. MultiBox now refuses such launches up front with that explanation. For apps of this class, MultiBox's category model calls the mechanism "pipe-brokered handoff".
- **Half-Life (and other GoldSrc games) open as real concurrent sessions.** `hl.exe` enforces one instance with an acquire-once launcher mutex (`ValveHalfLifeLauncherMutex`, verified by decompilation: `WaitForSingleObject` → "Only one instance of this game can be run at a time."), so that mutex is now on MultiBox's known single-instance list and is released like any other single-instance lock. The launcher also relays through the Steam client and exits when started bare, leaving nothing in the session — MultiBox now passes `-steam` (the argument Steam itself passes) so the engine runs inside the session's job, and starts Steam silently when it is missing, the same relay the launcher performs. Two concurrent Half-Life sessions verified end-to-end. Apps whose second copy hands off through a protected service instead of a local lock — Malwarebytes, for example — still cannot be duplicated: there is no mutex to release, and the lock lives in a SYSTEM service MultiBox cannot touch.
- **"The app executable could not be found" after a Store app update.** Saved profiles froze the full path to the app's executable, and Microsoft Store apps (Codex/ChatGPT, Claude) live in versioned `WindowsApps` folders that are deleted and replaced on every update — so clicking the profile in Vault failed after every update, and Store apps have no classic Start Menu shortcut for the old fallback to find. Launch paths now resolve to the current Store package location, versioned `app-<version>` install folders (Squirrel-style) heal the same way, a successful launch writes the new path back into the profile automatically, and Vault → Saved → Replace accepts a profile whose stored path went missing instead of refusing it.
- **Codex/ChatGPT → Open Folder** now starts in the real Windows user profile instead of the empty isolated Vault. Electron's folder dialog defaults to Downloads and then the profile folder; both were being redirected. Codex/ChatGPT now see the real profile root while `APPDATA`, `LOCALAPPDATA`, `TEMP`, `CODEX_HOME` and the Chromium user-data folder remain in the Vault, so sign-in, sessions and browser state stay per session.

## [1.0.4] - 2026-10-18

### Added
- **App compatibility layer** for apps that collide through things a profile folder can't separate:
  - OpenCode Desktop and Cline get their own background-service ports per session, fixing "Managed service port 45278 is already in use" and copies sharing Cline's hub.
  - Tauri apps (such as Cline) open as a real second copy instead of handing off to the open window.
  - WebView2 apps that keep data next to their `.exe` get their own data folder per session.
- Saved profiles in **Vault → Saved** now show when they were saved and how large they are.
- New sharp, vector-based app icon with dedicated 16–24 px versions and sizes for 125% / 150% display scaling.

### Fixed
- Starting an app normally from Windows could open the isolated copy instead (Tauri apps). Normal and isolated copies no longer capture each other's launches, in either order, and normal copies keep their own single-window behavior.
- **Save** reported "The app is still open" for Cline and Codex. Save now asks every real window to close (not a hidden helper window) and repeats the request if the app opens a new window while closing.
- **Save** failed on the first try with "Could not capture 'goals_1.sqlite'" when a database was briefly held right after the app closed. Database copies now retry short-lived holds safely.
- Saved profiles no longer include browser caches of apps that choose their own data folder, or OpenCode's 200 MB bundled CLI copy, which the app recreates.
- Opening **About** could shift the whole window sideways.
- Microsoft Store desktop apps (Codex, Claude) start in a session again.
- Reopening saved Chromium/Electron profiles no longer shows a "Profile error" dialog or rebuilds databases.

### Changed
- **Delete sessions & saved profiles…** moved into **Settings → More options**, next to the safer **Clear temporary sessions…**.
- README, architecture guide and website rewritten: step-by-step guide, compatibility table, FAQ, and an honest list of what MultiBox does not separate.

## [1.0.3] - 2026-10-17

First public release: isolated sessions with Windows Job Objects and per-session Vault folders, SaveBox profiles, Tasks panel, tray docking, and the project website.

[1.0.4]: https://github.com/uranrog-01/MultiBox/releases/tag/v1.0.4
[1.0.3]: https://github.com/uranrog-01/MultiBox/releases/tag/v1.0.3
