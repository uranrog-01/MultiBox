# Real Codex live-save findings

## Current result: selected close/save/reopen workflow works

The user chose to close only the selected app, keep its vault, save, and reopen it without an administrator prompt. This is implemented and passes the installed Codex test with 65 saved SQLite databases passing integrity checks. The project file restores and the same source vault reopens. Native tests cover 25 checks, including refusing to force an unsaved window closed and keeping data after copy or restart failures. See `restart-save-verification.md` and `restart-save-real-app.log`.

## Original uninterrupted live-copy failure

The screenshot's `declarative_performance_observer.db` failure was reproduced by a fixture before the policy change (`performance-before.log`). Chromium uses this store for performance reports and reporting policies:

https://chromium.googlesource.com/experimental/chromium/src/+/d17cbe483815707d8f862ecb557660dde0546d9e/content/browser/declarative_performance_observer/declarative_performance_observer_store.cc

The source now excludes that exact database and its SQLite sidecars only inside a recognized browser profile, recording the exclusions in the saved manifest. Similarly named backups and databases in ordinary app folders remain required data.

The copy pass now gathers blocked files across folders. Required data still causes the entire save to fail, retaining the last saved profile. Local diagnostics contain all blocked relative paths, error categories and codes; they do not contain file contents or credential values.

`performance-native.log`: 22 fixture checks pass, including locked performance-report data, Codex maintenance locks, project/conversation restore, committed WAL state, safe failed replacement, and multiple blockers in one attempt. These fixtures do not establish that a real running Codex profile can be saved.

## Real installed-app test

`tests/LiveAppProbe` is an opt-in test of a newly created session, separate from the normal test suite. It launches the installed executable through an owned `IsolationEngine`, waits 70 seconds for background storage to initialize, attempts a uniquely named save, and would restore a project canary and verify every saved SQLite database. Cleanup terminates only that engine's owned process job and vault; it never terminates by executable name or wipes existing user sessions. No saved profile was published by the failed test.

Target: `C:\Program Files\WindowsApps\OpenAI.Codex_26.1013.4894.0_x64__2p2nqsd0c76g0\app\ChatGPT.exe`.

**The real test fails after the optional-file fixes.** `performance-real-app.log` records 30 blocked files. They include:

- `Default/Network/Cookies` and its journal: Windows sharing violation.
- `Default/Login Data`, `Login Data For Account`, `Web Data`, `Account Web Data`: SQLite busy.
- Device Bound Sessions, Trust Tokens, QuotaManager and equivalent stores in the `codex-browser-app` partition.
- Additional browser stores such as first-party sets, affiliation data, favicons and segmentation data.

These include important sign-in and website data. They must not be added to a broad skip list. Ordinary shared file reads and SQLite's online-backup API cannot read these exclusively held stores while the app owns them. The original fixture tests passed because their important SQLite databases allowed concurrent readers; the fixture coverage was too narrow for this installed Chromium-based app.

## Next implementation decision

Two materially different behaviors are available:

1. Keep the target app open and add Windows volume snapshot backup with appropriate administrator approval, preserving a coherent on-disk source and its recovery sidecars. This is not implemented or validated yet. The current executor is not elevated. VSS requester privileges: https://learn.microsoft.com/en-us/windows/win32/vss/security-considerations-for-requestors
2. Explicitly close only the selected app while retaining its vault, save after it releases its files, and reopen it. Closing the selected app must be disclosed and consented to. Failed saves must keep its source vault. This is not automatic behavior in the current Save flow.

The user chose option 2. No elevation prompt, VSS snapshot or source-file unlock was attempted. Uninterrupted live copying remains unsupported for these locked stores; the implemented workflow closes and reopens the selected app.

REA static confirmation of the updated compiled file policy: `ev_086c783d96c90682ec5891af5f440fe723827c7ff694b6fdebe781cde4e4e2c2`, assembly SHA-256 `0356530049c7c728cae0aae86314a0c9d08c1666cbaa0f23216dcecd79f2716c`; selected members include `IsRegenerableBrowserFile` and `FileFailure`, and the performance-store filename is present. Runtime conclusions come from the native probe log, not the static inspection.
