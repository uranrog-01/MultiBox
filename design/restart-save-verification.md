# Save and reopen verification — MultiBox 1.0.2

Save in the app now uses `SessionManager.SaveProfileAndReopen` and the captured session ID. The name/replacement request is validated before closing anything. A running selected app receives normal close requests. Visible windows, including unsaved-work dialogs, must close before the workflow proceeds; the save aborts if they remain. Remaining background processes are stopped only through that session's owned Job Object, and process exit is checked before copying. The vault and its ownership lease remain alive throughout.

After copying, the same vault is reopened. An already closed session is saved and stays closed. Failed copies preserve the source vault and previous saved profile and still attempt reopening. A successful save with a failed reopen is reported as saved with a warning; the kept source can be reopened later. Failed process creation while resuming no longer disposes the existing vault.

Closed databases can perform normal SQLite journal recovery before online backup. The normal live-copy API retains read-only input behavior. There is no unverified raw-copy fallback for locked important databases.

The UI discloses the close/reopen behavior and asks the user to save work inside the target app first. It shows Closing, Saving and Reopening progress and ignores progress callbacks for other requests. Other apps are not closed or selected. No command file or administrator prompt is part of this flow.

## Checks

- `restart-save-native.log`: 25 passing integration checks. New cases verify selected-app targeting, other process IDs/selection unchanged, same-vault reopen, rollback-journal recovery excluding unfinished spilled pages, collision validation before close, failed-copy preservation/reopen, saved-with-reopen-failure semantics, source preservation after a bad executable launch, already-closed behavior, and a real test window that refuses normal shutdown. The window is not forced closed by Save.
- `restart-save-ui.log`: browser checks pass, including running/closed descriptions, Save & reopen, immutable request targeting, progress phases, stale callback rejection, duplicate submission protection, and guarded dismissal during saving. A queued dialog-close event cannot clear a newly opened save dialog.
- `restart-save-real-app.log`: the installed `OpenAI.Codex_26.1013.4894.0` `ChatGPT.exe` was launched in a fresh diagnostic engine, allowed 70 seconds to initialize, closed/saved/reopened, and restored into another vault. **All 65 saved SQLite databases passed `PRAGMA quick_check`**. The project canary restored and the original vault was kept and reopened. The diagnostic engine and uniquely named saved copy were cleaned up. Existing user apps were not terminated by name or included in the test's job.

This test uses a fresh profile, not a user's existing account. It validates saved database integrity and project-file restore, not every app's unsaved-memory behavior, cross-machine sign-in portability, or all applications' compatibility. Apps that refuse window closure require the user to finish their dialog before retrying Save.

## Packaged output

The final native rerun passed all 25 checks. The self-contained Windows x64 publish completed successfully and `publish/MultiBox.exe` was signed with the existing project signer. The root executable is version **1.0.2.0** and is byte-identical to the published executable. Root/publish copies of `UI/index.html`, `UI/profiles.js`, and `e_sqlite3.dll` also match. The previous root executable was retained under `design/ui-backup`.

Final executable SHA-256: `B8CB538B26A44137888B2B849034D5B574C24A67A8C6832733EDBCD1CF372631`.
