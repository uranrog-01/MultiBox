$ErrorActionPreference = 'SilentlyContinue'
function Scan($path) {
    $b = [IO.File]::ReadAllBytes($path)
    $t = [Text.Encoding]::ASCII.GetString($b)
    $u = [Text.Encoding]::Unicode.GetString($b)
    "=== $path ==="
    foreach ($p in @(
        'WinVerifyTrust','WldpQueryDynamicCodeTrust','CryptCATAdmin','CryptVerifyTimeStamp',
        'CertGetCertificateChain','saferComputeTokenFromLevel','CryptMsgClose',
        'ShellSmartScreenLevel','SmartScreenEnabled','checkforfileobe','smartscreen.microsoft.com',
        'ntsvc.urs','Zone.Identifier','MarkOfTheWeb','urlmon.dll','POLICY','SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer'
    )) {
        if ($t.IndexOf($p, [StringComparison]::OrdinalIgnoreCase) -ge 0) { "FOUND(ascii): $p" }
        elseif ($u.IndexOf($p, [StringComparison]::OrdinalIgnoreCase) -ge 0) { "FOUND(utf16): $p" }
    }
    "=== URLs (ascii) ==="
    [regex]::Matches($t, 'https?://[A-Za-z0-9._/\-]+') | ForEach-Object { $_.Value } |
        Sort-Object -Unique | Select-Object -First 15
}
Scan 'C:\Windows\System32\consent.exe'
Scan 'C:\Windows\System32\smartscreen.exe'
