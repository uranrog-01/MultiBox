# Pipe-brokered single-instance apps — full architecture reconstruction

> Reverse-engineered from Malwarebytes 5.1.0.1885 (`Malwarebytes.dll`, `MbamUI.Services.dll`,
> `MbamUI.Core.dll`; .NET 10, IL-level metadata + method-body analysis, target never executed).
> This documents the app class whose second copy cannot be opened by profile separation alone,
> and how MultiBox's core handles it.

## 1. The lock machinery, decompiled

The whole mechanism lives in one class: `MbamUI.Services.SingleInstanceService`
(`MbamUI.Services.dll`, 14 methods; source paths in its log strings point to
`src\MbamUI.Services\Services\SingleInstanceService.cs`). Hosted by `MbamUI.App`
(property `SingleInstanceService`, created by `MbamUI.Program.CreateSingleInstanceService`
during startup, wrapped in a startup benchmark).

### 1.1 The name (`GenerateSemaphoreName` — exact IL semantics)

```csharp
string GenerateSemaphoreName()
{
    var guid = new Guid("B1CDD42D-DA9D-4063-A195-76AF6FED2554");   // hard-coded literal (UPPERCASE in the binary)
    return $"MbamUI_{guid}_{WindowsIdentity.GetCurrent().User}";   // process account SID, e.g. S-1-5-21-…-1001
}
```

- The GUID literal is constant per app version (found in `MbamUI.Services.dll` only —
  uppercase; a lowercase grep misses it).
- The SID comes from the **process token** (`WindowsIdentity.GetCurrent().User`), so it varies
  **only with the Windows account**.
- No environment variable, registry value, file, or profile folder is consulted. Verified by
  full-IL read of every method involved, plus a live experiment: a session copy in a fresh,
  empty Vault produced the byte-identical name.

### 1.2 First copy (`.ctor` → `CreatePipeServerAndStartListening`)

```csharp
SingleInstanceService()
{
    // fields: TaskCompletionSource, CancellationTokenSource, serializer
    semaphore = new Semaphore(initialCount: 1? , maximumCount: 1?, GenerateSemaphoreName(), out _createdNew);
    if (!semaphore.WaitOne(<zero-or-short timeout>))
        throw new InvalidOperationException(
            "Failing waiting the semaphore as too many instances already started");
    CreatePipeServerAndStartListening();
}

CreatePipeServerAndStartListening()
{
    serverCreationTask = Task.Run(ListenForArgumentsAsync);   // see 1.3
    serverCreationTask.Wait();                                // waits for the pipe to be created
}
```

The gate is the **named semaphore** (session-local, `\Sessions\<n>\BaseNamedObjects\MbamUI_…`).
The first copy acquires it and holds it until `Dispose`.

### 1.3 The server (`ListenForArgumentsAsync` async state machine)

```csharp
async ListenForArgumentsAsync()
{
    var security = new PipeSecurity();
    security.AddAccessRule(new PipeAccessRule(
        WindowsIdentity.GetCurrent().User,              // ACL: current user only
        PipeAccessRights.FullControl /*observed shape*/, AccessControlType.Allow));
    var server = NamedPipeServerStreamAcl.Create(       // ACL-aware server creation
        GenerateSemaphoreName(), Inheritance: none, 1 /*instances*/, …);

    while (true)
    {
        await server.WaitForConnectionAsync(token);
        // read arguments line from the client; raise App.OnArgumentsReceived(args)
        // → App shows MainWindow ("First instance will display MainWindow.")
    }
}
```

`App` subscribes via `SingleInstanceService.OnArgumentsReceived`; the received string is the
second copy's command line (the UI log records it: `Received arguments from second instance
'"…"'; … First instance will display MainWindow.`).

### 1.4 Second copy (`SendArguments`)

```csharp
SendArguments(string arguments)
{
    var client = new NamedPipeClientStream(".", GenerateSemaphoreName(), Out);
    client.Connect();                                  // succeeds: the FIRST copy owns the name
    using var writer = new StreamWriter(client) { AutoFlush = true };
    writer.WriteLine(arguments.Trim());                // forward own command line
    // returns → the second copy exits with code 0; the first window comes to front
}
```

Failure of any step surfaces as `MessageBox "Unexpected error while signaling first instance."`.
A related command channel exists (`SendPipeErrorCommand` writes `"--<command>"` lines, e.g.
`--StopService` handled by `App.OnExitUIRequested`; `App.HandleSecondInstancePipeError` logs
`Received SecondInstancePipeError command from another instance! […]. MBAMUI instance in
SessionId [ … ] exiting!` — the SessionId is the receiving process's own).

### 1.5 Shutdown (`Dispose`)

`CancellationTokenSource.Cancel` → `Semaphore.Release()` (guarded against
"Semaphore full exception during release") → `WaitHandle.Dispose`.

### 1.6 Startup wrapper (`MbamUI.Program.CreateSingleInstanceService`)

```csharp
CreateSingleInstanceService()
{
    benchmark("Create SingleInstanceService");
    var attempts = …;                                  // bounded retry loop
    while (attempts--)
    {
        var service = new SingleInstanceService();     // semaphore gate (1.2)
        try { service.CreatePipeServerAndStartListening(); return service; }
        catch { service.Dispose(); }                   // retry the SAME name
    }
    MessageBox.Show("Too many UI instances started !", "Fatal error during startup");
}
```

The retry loop re-uses the **same** deterministic name every attempt — it only tolerates
transient failures; it can never produce a second concurrent UI.

## 2. Why profiles cannot defeat this (the Codex/Claude difference)

- **Electron/Chromium apps** (Codex, Claude, Discord…) enforce single instance with a lock
  **inside the user-data directory**. Point a second copy at a different data dir and the lock
  is simply elsewhere → both run. MultiBox's Vault does exactly that (plus explicit engine
  flags), so per-profile instances are the natural result.
- **Pipe-brokered apps** enforce it with a **machine-global named object whose name never
  reads the profile**. MultiBox still gives every session a fully separate Vault — but the
  app computes its lock name from (a) a constant baked into the binary and (b) the process
  account SID. Verified: fresh Vault → byte-identical name; different Vault environments →
  identical name (two vaulted copies collided); registry/ProgramData contain no relevant
  config (17-line registry dump, no instance-related values; GUID absent from every file).

The only input that varies is the **account SID** → the only legitimate second window is under
a **different Windows account**. No user-mode tool can change a name the app derives from its
own constant + its process token without patching or injecting the app — which MultiBox never
does.

## 3. How MultiBox core handles the class (`Core/AppCompatibility.cs`, `Core/JobObject.cs`)

| Situation | Behavior |
| :--- | :--- |
| App not in the known list, no copy running | Normal vaulted launch (the copy becomes the first instance and works) |
| Known pipe-brokered app, copy already running, **same account** | Launch refused **before** starting with a full explanation (`PipeBrokeredSingleInstanceBlocked`, marker `one window per Windows account`); the toast offers **"Different user…"** |
| Known pipe-brokered app, **different account requested** | Refusal skipped (different SID → different pipe name); `JobObject.StartAs` launches via `CreateProcessWithLogonW` with `LOGON_WITH_PROFILE`, keeps the **Vault environment block** and assigns the process to the **session job**, so the second copy's profile data still lands in its Vault |
| Reopen/Save-reopen of such a session | The account (and arguments/working directory) are retained by the engine and reused |

Known pipe-brokered apps today: `malwarebytes.exe` (the list is extensible per app after
verification — the mechanism above is the verification checklist: name construction → gate →
client handoff → retry semantics).

## 4. Verification checklist used for Malwarebytes (reusable for future apps)

1. Managed P/Invoke inventory (`inspect_managed_native_boundaries`): a *pure managed* check
   has ~no native imports — don't hunt mutexes with native tools only.
2. Metadata strings for the service type and protocol (`SingleInstanceService`,
   `HandleSecondInstancePipeError`, pipe-command literals).
3. Live **all-type** named-handle dump of a first instance (File/Event/Semaphore/Section/ALPC —
   a mutex-only probe misses pipe servers, which are `File` handles under `\Device\NamedPipe\`).
4. The app's own log (`MBAMUI.LOG`) for the handshake wording.
5. Name-constant search **case-insensitive** across all sibling assemblies + registry
   **values** (recursive) + ProgramData.
6. IL trace of the name construction and the gate (this document, section 1).
7. Live experiment: fresh-profile first instance must produce the byte-identical name
   (proves profile-independence) and a second copy must hand off (proves the gate).
