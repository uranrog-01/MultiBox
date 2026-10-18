# Contributing to MultiBox

Thanks for helping. Bug reports, app compatibility reports and pull requests are all welcome.

## Reporting

- **An app doesn't work in MultiBox?** Use the [app compatibility form](https://github.com/uranrog-01/MultiBox/issues/new?template=app_compatibility.yml). The app's name, version and install folder help most.
- **Something in MultiBox is broken?** Use the [bug report form](https://github.com/uranrog-01/MultiBox/issues/new?template=bug_report.yml).
- **Security problem?** Follow [SECURITY.md](SECURITY.md) instead of opening an issue.

## Building

Requires Windows 10/11 x64 and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build -c Release
```

Run the native integration tests (they start real processes and windows, so run them on a desktop session):

```powershell
dotnet build tests/SessionFixture -c Release
dotnet run --project tests/SessionTests -c Release -- tests/SessionFixture/bin/Release/net10.0-windows
```

Build a release zip in `dist\`:

```powershell
pwsh -File build-release.ps1
```

## Where things live

| Area | Files |
| :--- | :--- |
| Launching, environment, Job Objects | `Core/IsolationEngine.cs`, `Core/JobObject.cs` |
| Per-app fixes (ports, single-instance locks) | `Core/AppCompatibility.cs` |
| Saving and restoring profiles | `Core/SaveBoxManager.cs`, `Core/LiveProfileSnapshot.cs`, `Core/PathMigrationHelper.cs` |
| Interface (WebView2) | `UI/` |
| Website | `docs/` |
| App icon (edit the SVG, then run the script) | `design/icon/multibox-icon.svg`, `design/icon/build-icon.py` |

See [ARCHITECTURE.md](ARCHITECTURE.md) for how the pieces fit together.

## Pull requests

- Keep each pull request to one change, and describe how you tested it.
- Match the surrounding code style.
- For new per-app fixes, add the app to the compatibility table in `README.md` and `ARCHITECTURE.md`.
