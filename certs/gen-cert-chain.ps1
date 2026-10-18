# Generates the zer0ne certificate chain used to sign MultiBox:
#   zer0ne Root CA (15y, CA:true)  ->  zer0ne Code Signing leaf (5y)
# Certificates land in CurrentUser\My; exports in .\certs\
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path certs | Out-Null

$root = New-SelfSignedCertificate `
    -Type Custom `
    -Subject "CN=zer0ne Root CA,O=zer0ne,E=https://github.com/uranrog-01" `
    -KeyExportPolicy Exportable `
    -KeyUsage CertSign, CRLSign, DigitalSignature `
    -KeyLength 4096 -KeyAlgorithm RSA -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(15) `
    -CertStoreLocation Cert:\CurrentUser\My `
    -TextExtension @("2.5.29.19={critical}{text}ca=true") `
    -FriendlyName "zer0ne Root CA"

$leaf = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=zer0ne,O=zer0ne,E=https://github.com/uranrog-01" `
    -Signer $root `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 3072 -KeyAlgorithm RSA -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation Cert:\CurrentUser\My `
    -TextExtension @("2.5.29.19={text}ca=false") `
    -FriendlyName "zer0ne Code Signing"

Export-Certificate -Cert $root -FilePath certs\zer0ne-root-ca.cer | Out-Null
Export-Certificate -Cert $leaf -FilePath certs\zer0ne-codesigning.cer | Out-Null

Write-Host "Certificates generated successfully in Cert:\CurrentUser\My"
Write-Host "ROOT_THUMB=$($root.Thumbprint)"
Write-Host "LEAF_THUMB=$($leaf.Thumbprint)"
