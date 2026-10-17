# Live SaveBox investigation

The reported failure names `DIPS`. Chromium uses this database for bounce-tracking timestamps and can open it with exclusive locking. It is separate from project files, preferences, cookies and app local storage.

Primary references:
- https://chromium.googlesource.com/chromium/src/+/main/content/browser/btm/README.md
- https://chromium.googlesource.com/chromium/src/+/317651c8a55b5c14f07117d37d87bbc7ac2ea93a/chrome/browser/dips/dips_database.cc (constructor sets exclusive locking)

REA MCP static inspection was performed on the existing compiled `MultiBox.dll`, selecting `MultiBox.Core.LiveProfileSnapshot`.

- Evidence: `ev_edda0760818cf40bdd6dc21497c8bf4cce57b725fa2544f271d9fce0fe019aeb`
- Artifact SHA-256: `360b2bbfebf849246f8029645bd1310fcd98a8b7ef1bc1343c2b726ec0237f57`
- Module MVID: `21756b42-67ff-4e6d-ad49-43b42a552369`
- Inspected members include `CopyDirectory`, `IsMarker`, `IsNonCriticalEphemeralFile`, `BackupSqlite`.
- The existing binary includes broad `dips`, `.bak`, `.tmp` and `.temp` filtering literals. Source review confirms broad prefix filtering and a raw database-copy fallback after online-backup errors.
- Static REA inspection does not execute the app or prove its runtime behavior. No running MultiBox process or live session folder was visible to the command environment during this investigation. The screenshot's session has not been saved by this investigation.

The integration fixture now holds a real SQLite DIPS database in exclusive mode and holds its sidecars open while saving. It also contains project HTML, preferences and ordinary `DIPS-project-notes.txt` app data.

The regression initially fails because the existing broad DIPS prefix rule silently removes `DIPS-project-notes.txt`. See `dips-before.log`. A fix must exclude only exact DIPS database/sidecar names within a recognized Chromium profile, retain similarly named durable files, and fail safely for unreadable important data. A successful snapshot must restore the project and keep the original process IDs running.

## Implemented and verified

- Only `DIPS`, `DIPS-wal`, `DIPS-shm` and `DIPS-journal` are excluded, inside directories identified by Chromium profile markers under the vault's AppData. Both Electron's root and Chrome's Default profile are covered. DIPS in ordinary app data or Documents is retained; DIPS-prefixed project notes are retained.
- Exclusions are recorded in the saved manifest's `RegeneratedBrowserMetadata`; restored profiles start fresh bounce-tracking history. Cookies, settings, local storage and projects are not classified as DIPS metadata.
- Important unreadable files fail the save while leaving the previous saved copy intact. No raw-copy fallback is used for an important exclusively locked SQLite database.
- Profile recovery takes the shared SaveBox mutex before touching staging directories. It leaves active saves alone. Saves/deletes never proceed when they time out acquiring that lock.
- `dips-native.log`: all 19 integration checks pass. The fixture holds live SQLite WAL changes, real exclusive DIPS locks and file locks. Tests verify project source, preferences and local storage survive; unfinished SQLite transactions stay out; restore uses independent data; original process IDs remain running. Another fixture process holds the shared mutex to verify active staging survives profile listing and an overlapping save times out safely.
- REA inspection of the updated compiled saver: `ev_4c4e6ef5cce481352e70b20d4336e9145f0c9b97148431b5ea2d415f22ccc0ac`, SHA-256 `726dc531b9ecd36223881c6718ee139c974f409de655604eacf61c847751e28e`. The selected member inventory includes `IsDipsFile` and `IsChromiumProfileDirectory`; the broad `IsNonCriticalEphemeralFile` classifier is removed. Runtime claims above come from integration tests, not static inspection.

For a session owned by an older running MultiBox, use the updated `Save open profile.cmd` helper, select that session and app, then save under a new name. This does not close the app or start session cleanup. A user's currently open screenshot session was not visible in this executor and has not been saved automatically. SaveBox archives on-disk vault data; it cannot capture unsaved app memory or projects located outside the vault.

Packaging completed: `dotnet publish` succeeds (`dips-publish.log`). The executable was signed using the project's existing zer0ne certificate (`dips-sign.log`); Windows reports `UnknownError` for Authenticode trust validation in this environment, so trusted-publisher status is not asserted. Root and publish executable hashes match: `0E2472A4442168DAFC2F1160BA62BB17EC82148A3D04689B01C52F8A6B7A1569`. Native SQLite runtime and UI hashes also match. The previous root executable is preserved under `design/ui-backup`. No user's app was closed or restarted.
