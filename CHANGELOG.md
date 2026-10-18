# Changelog

All notable changes to MultiBox. Versions follow [Semantic Versioning](https://semver.org/).

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
