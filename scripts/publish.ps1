# สร้างไฟล์สำหรับทำ installer (self-contained, ไม่ต้องลง .NET runtime)
#
#   powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
#
# ผลลัพธ์: publish\  (OpenKey.exe + DLL + config.json)
# เอาโฟลเดอร์นี้ไปเข้า Inno Setup / WiX / MSIX ต่อได้เลย

param(
    [string]$Output = "publish",
    [switch]$SingleFile
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $arguments = @(
        "publish", "src/OpenKey/OpenKey.csproj",
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-o", $Output
    )
    if ($SingleFile) {
        $arguments += "-p:PublishSingleFile=true"
        $arguments += "-p:IncludeNativeLibrariesForSelfExtract=true"
    }

    Write-Host "dotnet $($arguments -join ' ')" -ForegroundColor Cyan
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "publish ล้มเหลว (exit $LASTEXITCODE)" }

    # config.json ต้องอยู่ข้าง exe เพื่อให้ผู้ใช้แก้ได้
    Copy-Item config.json (Join-Path $Output "config.json") -Force

    $size = (Get-ChildItem $Output -Recurse -File | Measure-Object Length -Sum).Sum
    Write-Host ("เสร็จแล้ว: {0} ({1:N1} MB)" -f $Output, ($size / 1MB)) -ForegroundColor Green
}
finally {
    Pop-Location
}
