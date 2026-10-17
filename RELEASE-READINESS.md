# MultiBox release-readiness review

Reviewed: October 17, 2026. Scope: the local Windows project and its current win-x64 publish output. No public upload or deployment was performed. Application source and the executable were not changed during this review; local audit scripts and evidence were added.

**Verdict: keep this build private. Fix the security and runtime issues before a public beta. A stable release also needs saved-profile recovery, clean-machine validation and release packaging.**

**Later save validation:** uninterrupted live copying failed against the installed Codex app with 30 locked files, including Cookies and Login Data. The user selected a close/save/reopen workflow instead. That workflow now passes the real installed-app test: the same vault reopens, a project file restores, and all 65 saved SQLite databases pass integrity checks. The native suite passes 25 checks, including failed copy/reopen and unsaved-window guards. See `design/restart-save-verification.md`. This validates the chosen save behavior, not general uninterrupted live backup or the other release issues in this review.

## Evidence that is already positive

- The latest native integration run passed 15 checks: process-tree separation and handoff, five concurrent sessions, targeted wipe, named restore/replacement, failed-save preservation, legacy loading, generated-runtime exclusions, long-path data, indicator rendering/timing and abandoned-vault recovery.
- Profiles, session-switching and general UI browser checks passed again. These use a simulated native bridge, rather than end-to-end automation of the shipped WebView2 host.
- The current NuGet vulnerability query reports no vulnerable packages for the configured sources. This covers the referenced WebView2 package; it does not establish that the bundled .NET runtime is current.
- A limited credential-pattern scan of 32 source/UI files found no matches. Archives, binaries, user profiles and Git history were not scanned. This is not a comprehensive secret scan.
- Root and publish executables have matching SHA-256 hashes.

## Fix before a public beta

### 1. Confirmed Tasks-panel script injection — high priority

Location: `UI/index.html:2084` and `UI/index.html:2110`.

Process names and executable paths enter a generated HTML `onclick` attribute. `escapeJsString` escapes literal apostrophes and backslashes but does not protect the surrounding HTML context. An HTML-entity sequence in a valid executable filename becomes a JavaScript quote when the browser parses the attribute.

The local audit rendered such a process through the real Tasks renderer, clicked its button and confirmed execution of a harmless boolean marker. The native bridge was simulated; no actual executable or destructive command was run. Reproduction: `node design/audit-release-ui.cjs`; evidence: `design/release-ui-audit.json`.

The demonstrated trigger requires a crafted local process entry and a button click. This is a confirmed UI injection, not proof of a remotely reachable exploit or a privilege escalation. The injected script would execute in the same UI context that can send native launch/delete commands.

Fix: construct cards with DOM APIs, assign strings through `textContent`, and attach callbacks with `addEventListener`. Do not interpolate process metadata into HTML event-handler code. Add a regression test covering entity-encoded filenames and paths.

### 2. The native WebView2 boundary is not restricted

Location: `MainForm.cs:234` and `MainForm.cs:303`.

The message handler consumes actions without checking `e.Source`. Navigation/frame navigation and new windows are not restricted to the packaged UI. Release builds also enable DevTools. These are distinct from the confirmed injection: trusting an allowed origin alone would not fix injection inside that origin.

Fix: allow only the exact trusted UI document/origin, reject other message sources, block unexpected top-level/frame navigations and new windows, validate message schemas/explicit session targets, and disable DevTools in Release. Test rejected origins and malformed/destructive messages. A suitable content-security policy is additional defense after removing inline handlers.

Microsoft recommends checking document/message origins and controlling navigation: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security

### 3. The self-contained runtime is behind current security patches

Evidence: `obj/Release/net10.0-windows/win-x64/MultiBox.deps.json:16` and `:17` list the .NET and Windows Desktop runtime packs as **10.0.5**. The publish output embeds that runtime.

Microsoft's current .NET 10 download page lists **10.0.13**, released October 13, 2026, as a security patch. Update the build SDK/runtime packs, republish, inspect the resulting dependency manifest, and rerun tests. A newer runtime installed on the recipient's PC does not replace the runtime embedded in this self-contained package.

Source checked October 17, 2026: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## Fix before calling the release stable

### 4. Saved-profile replacement needs recovery and cross-process coordination

Location: `Core/SaveBoxManager.cs:19`, `:37`, and `:94`–`:100`.

Replacement first moves the old profile into a hidden backup directory and then moves staging into the visible destination. The exception path rolls back ordinary failures, but a process/power failure between these operations bypasses that rollback. The library skips `.multibox-*` folders, and startup recovery skips SaveBox, so the recoverable old copy can disappear from the library until manually recovered. This is a source-identified crash window; the review did not kill a process during an actual saved-profile commit.

The store lock is a .NET object and only coordinates one process. There is no single-instance guard in `Program.cs`; separate MultiBox instances can write the same library. Test simultaneous writes/replacements and interrupted commits, then add a small cross-process lock and recoverable commit journal/backup protocol, or deliberately route launches through one application instance.

### 5. Real-app and clean-machine compatibility is not established

The tests run controlled local fixtures and browser simulations. They do not prove that current Chrome, Discord, ChatGPT or every supported executable honors the redirected profile, keeps host sign-in state untouched, or restores its sign-in data correctly.

Before stable release, document a small tested-app matrix on clean supported Windows machines, using disposable test accounts. Check two concurrent profiles, normal app close, tray exit, host crash, saved restore, focus/indicator behavior, non-English/long paths and normal user permissions. Check a machine without WebView2 Runtime, too.

The current startup catches a missing-runtime error and shows a generic initialization message; it has no prerequisite installer or guided recovery. Microsoft recommends detecting and installing the Runtime when absent: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution

## Prepare the online release

- Choose and include a project license/distribution terms, and include applicable dependency notices. None were found at the project root or in publish output during this review. This review does not establish rights to all image/reference assets.
- Set explicit release/version metadata and publish release notes, Windows/architecture requirements, a support route and checksums. Current executable metadata defaults to `1.0.0`.
- Build a clean release archive from an allowlist of required files, including the UI, executable and required loader assets. Exclude old RAR files, backup executables, `bin`, `obj`, developer settings, PDB/XML documentation, local logs and private design references. Do not upload the whole workspace or the existing unreviewed RAR archives.
- The current executable is **not digitally signed**. Signing is strongly recommended for broad distribution; it is not, by itself, proof of safety or a guarantee against warnings.
- Make source tests portable: browser scripts currently hard-code this machine's bundled Playwright path. There is no repository/CI configuration or dependency setup for another contributor to reproduce them.
- Resolve the existing WindowsBase/WebView2 WPF assembly warning before treating builds as clean. Current builds succeed despite it.

## Product claims and privacy

`README.md` correctly describes profile separation and says it is not an air gap or a boundary against malicious apps. Keep that positioning.

The older root `readme.html:686` still promises wiping 100% of session files/registry keys and zero traces. That claim is unsupported: this implementation redirects profile paths and manages process trees; it does not virtualize all filesystem/registry access or erase remote/server logs. Remove or rewrite the old page before publishing source or using it as website content.

Saved profiles are ordinary copied files; MultiBox does not encrypt them. Depending on the app they can contain sign-in tokens and other sensitive data. Disclose their storage path, persistence and deletion behavior. Do not market SaveBox as an encrypted credential vault. A local scan found no application analytics/network-upload client in the reviewed application code, but launched apps and the WebView2 platform have their own network behavior.

## Smallest sensible release sequence

1. Fix and regression-test the Tasks renderer and WebView2 trust boundary.
2. Update the embedded runtime and produce a clean, versioned package with license/notices and corrected claims.
3. Verify real apps and first-run prerequisites on clean Windows machines.
4. Share a clearly labeled beta; repair/test saved-profile crash recovery and simultaneous writes before a stable release.

An auto-updater, cloud accounts, telemetry service, large installer or more visual features are not prerequisites for this first release.
