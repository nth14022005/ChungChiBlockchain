param([Parameter(Mandatory=$true)][System.Net.IPAddress]$LanIP)
$ErrorActionPreference='Stop'
if ($LanIP.AddressFamily -ne 'InterNetwork' -or -not (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -eq $LanIP.IPAddressToString -and $_.IPAddress -ne '127.0.0.1' })) { throw 'Nhap IPv4 cua card Wi-Fi/Ethernet tren may nay (xem ipconfig).' }
$web=Join-Path $PSScriptRoot 'src\ChungChiBlockchain.Web'
$base="http://$($LanIP.IPAddressToString):5087"
$oldBase=$env:PublicBaseUrl
Push-Location $web
try {
    $env:PublicBaseUrl=$base
    Write-Host "Lien ket dien thoai: $base/verify"
    & dotnet run --launch-profile http -- --urls "http://localhost:5087;$base"
    if ($LASTEXITCODE -ne 0) { throw 'Web khoi dong that bai. Kiem tra cong 5087.' }
} finally { $env:PublicBaseUrl=$oldBase; Pop-Location }
