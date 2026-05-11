param(
    [Parameter(Mandatory = $true)]
    [string]$Hwid,
    [Parameter(Mandatory = $true)]
    [string]$Holder,
    [ValidateSet("Day", "Week", "Month")]
    [string]$Duration,
    [string]$Output = ".\\March7thHoney.license"
)

$ErrorActionPreference = "Stop"

function Get-LicenseKeyBytes {
    $keyText = "March7thHoney.Public.License.Internal.Key"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($keyText))
    } finally {
        $sha.Dispose()
    }
}

function Encrypt-LicenseJson([string]$jsonText) {
    $key = Get-LicenseKeyBytes
    $aes = [System.Security.Cryptography.Aes]::Create()
    try {
        $aes.Key = $key
        $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
        $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
        $aes.GenerateIV()

        $encryptor = $aes.CreateEncryptor()
        try {
            $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($jsonText)
            $cipherBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
        } finally {
            $encryptor.Dispose()
        }

        $packed = New-Object byte[] ($aes.IV.Length + $cipherBytes.Length)
        [System.Buffer]::BlockCopy($aes.IV, 0, $packed, 0, $aes.IV.Length)
        [System.Buffer]::BlockCopy($cipherBytes, 0, $packed, $aes.IV.Length, $cipherBytes.Length)
        return "M7H1:" + [Convert]::ToBase64String($packed)
    } finally {
        $aes.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($Duration)) {
    Write-Host "Select license duration:"
    Write-Host "  1) 1 day"
    Write-Host "  2) 1 week"
    Write-Host "  3) 1 month"
    $selection = Read-Host "Enter choice (1/2/3)"
    switch ($selection) {
        "1" { $Duration = "Day" }
        "2" { $Duration = "Week" }
        "3" { $Duration = "Month" }
        default {
            Write-Host "Invalid choice. Defaulting to 1 month."
            $Duration = "Month"
        }
    }
}

switch ($Duration) {
    "Day" { $validDays = 1 }
    "Week" { $validDays = 7 }
    "Month" { $validDays = 30 }
    default { $validDays = 30 }
}

$issuedAt = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$expireAt = [DateTimeOffset]::FromUnixTimeSeconds($issuedAt).AddDays($validDays).ToUnixTimeSeconds()
$licenseObj = [ordered]@{
    hwid = $Hwid.Trim()
    issuedAt = $issuedAt
    trialDays = $validDays
    expireAt = $expireAt
    holder = $Holder.Trim()
}

$json = $licenseObj | ConvertTo-Json -Compress
$encrypted = Encrypt-LicenseJson -jsonText $json

$outputPath = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Output))
$outputDir = Split-Path -Parent $outputPath
if (-not [string]::IsNullOrWhiteSpace($outputDir) -and -not (Test-Path -Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Set-Content -Path $outputPath -Value $encrypted -NoNewline

Write-Host "License generated:"
Write-Host "  Holder: $Holder"
Write-Host "  HWID: $Hwid"
Write-Host "  Duration: $Duration ($validDays day(s))"
Write-Host "  IssuedAt(Unix): $issuedAt"
Write-Host "  ExpireAt(Unix): $expireAt"
Write-Host "  Output: $outputPath"
