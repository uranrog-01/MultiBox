# Security policy

## Reporting a problem

Please **don't open a public issue** for security problems. Report them privately through
[GitHub's private vulnerability reporting](https://github.com/uranrog-01/MultiBox/security/advisories/new).

Include what you found, how to reproduce it, and the MultiBox version (Settings → bottom left). You'll get a reply within 7 days.

## Supported versions

Only the [latest release](https://github.com/uranrog-01/MultiBox/releases/latest) receives fixes.

## What MultiBox is, and isn't

MultiBox separates app **profiles** (logins, cookies, settings) between copies of an app. It is **not a security sandbox**: apps run with your normal Windows permissions, can read your files, and share the registry and Windows Credential Manager. Don't use it to contain untrusted software; use a virtual machine or Windows Sandbox for that.

Reports about apps reading data outside their session are expected behavior, not vulnerabilities. Reports where MultiBox itself deletes, exposes or corrupts data outside a session are very welcome.

## Releases

Releases are signed with a self-signed certificate (`CN=zer0ne`), and each release lists the SHA-256 of its zip. Compare it with:

```powershell
Get-FileHash .\MultiBox-v1.0.4-win-x64.zip -Algorithm SHA256
```
