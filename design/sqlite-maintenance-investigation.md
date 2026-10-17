# Codex maintenance marker blocking live saves

The screenshot reports `.sqlite-maintenance.lock`, a separate problem from Chromium DIPS. Codex's maintenance worker opens that exact filename under its SQLite home and holds an OS file lock to elect one worker. It does not write project or conversation state into that marker.

Primary source: https://github.com/openai/codex/blob/main/codex-rs/state/src/runtime/reclamation.rs (`try_ownership`, including creation without truncation and `try_lock`).

REA MCP inspected the compiled `MultiBox.Core.LiveProfileSnapshot` before and after the change:

- Before evidence: `ev_ca034e899d4290bca7d25fc0ca317610772bd86b12561153d25135352a1c8511`; assembly SHA-256 `c78d69bb19dfc5d274db8a8b7e856772a485688ffd963f7e5f0d3fd06ec08194`. The maintenance marker was absent from the selected type's marker literals.
- After evidence: `ev_f7158a88337b5f0cb403ee2e97dffd9bbee6db6867df2b1e60d6a42bf19e3eb1`; assembly SHA-256 `0103b92b1fd5a81f233d11e3fa64e45466014a7217c54ce3ce1cd5f928131e1c`. The compiled saver includes `IsCodexMaintenanceLock` and the exact `.sqlite-maintenance.lock` literal.
- REA's native Windows process-capture provider returned `capability_unavailable`: it does not verify descendant cleanup on this platform. No runtime capture is claimed. Runtime verification below used the project's native test runner instead. REA static inspection does not execute the target assembly.

The old marker classifier only recognized app locks under AppData. MultiBox sets `CODEX_HOME` to `User/.codex`, outside AppData. The file was therefore treated as required profile content and reading the held marker failed.

The fix classifies the exact marker before the AppData guard. It accepts canonical Codex homes (`User/.codex`, Roaming/Codex, Roaming/ChatGPT, Local/OpenAI/Codex), or nested homes within those branches that contain Codex state/log SQLite files. It does not skip arbitrary `.lock` files, similarly named backups, or this exact filename in ordinary project folders. It neither deletes nor unlocks the running app's marker. The database is still saved through SQLite's online-backup API.

`maintenance-before.log` reproduces the screenshot's exact failure through `SessionManager.SaveProfile`, the route used by the in-app Save button. The fixture holds `.sqlite-maintenance.lock` open, a live `state_5.sqlite` WAL connection with committed and uncommitted state, and conversation/project files.

`maintenance-native.log`: all 20 integration checks pass after the fix. Tests verify the marker is omitted from the copy but remains present and held in the live session; committed SQLite state passes `quick_check`; uncommitted data stays out; conversation history, project HTML and similarly named files survive save/restore; an unreadable ordinary project file still fails safely; app process IDs stay running. Existing DIPS, profile replacement, recovery and cross-process save checks also pass.

`maintenance-publish.log`: publish succeeded. Root and publish executable, UI and SQLite runtime hashes were compared. The executable was signed using the existing project certificate (`maintenance-sign.log`); no certificate trust settings were changed. No user's app was closed, restarted or wiped. The saved screenshot session was not captured automatically; no live vault was available at the final inventory. A running older executable retains its already-loaded code until relaunched.
