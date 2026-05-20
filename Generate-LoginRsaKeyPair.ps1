param(
    [int]$KeySize = 2048,
    [string]$ServerSecurityDir = (Join-Path $PSScriptRoot "Config\Security")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-ToPem {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][byte[]]$Bytes
    )

    $base64 = [Convert]::ToBase64String($Bytes, [Base64FormattingOptions]::InsertLineBreaks)
    return "-----BEGIN $Label-----`n$base64`n-----END $Label-----`n"
}

if ($KeySize -lt 2048) {
    throw "Use a key size of at least 2048 for HSR login compatibility."
}

$serverDir = [System.IO.Path]::GetFullPath($ServerSecurityDir)
[System.IO.Directory]::CreateDirectory($serverDir) | Out-Null

$privatePath = Join-Path $serverDir "LoginRsaPrivateKey.pem"
$publicPath = Join-Path $serverDir "LoginRsaPublicKey.pem"

$rsa = [System.Security.Cryptography.RSA]::Create($KeySize)
try {
    $privatePem = Convert-ToPem "RSA PRIVATE KEY" ($rsa.ExportRSAPrivateKey())
    $publicPem = Convert-ToPem "PUBLIC KEY" ($rsa.ExportSubjectPublicKeyInfo())

    [System.IO.File]::WriteAllText($privatePath, $privatePem, [System.Text.Encoding]::ASCII)
    [System.IO.File]::WriteAllText($publicPath, $publicPem, [System.Text.Encoding]::ASCII)

    Write-Host "Wrote server private key: $privatePath"
    Write-Host "Wrote server public key : $publicPath"
}
finally {
    $rsa.Dispose()
}
