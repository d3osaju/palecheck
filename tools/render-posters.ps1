# Renders the social-preview image and Devpost gallery images from the running app.
# Usage: start the app (dotnet run --project src/PaleCheck.Web --launch-profile http), then
#        powershell -File tools/render-posters.ps1 [-Base http://localhost:5049]
param([string]$Base = "http://localhost:5049")

$browser = @(
    "C:\Program Files\Google\Chrome\Application\chrome.exe",
    "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $browser) { throw "Chrome or Edge not found" }

$root = Split-Path $PSScriptRoot -Parent
$gallery = Join-Path $root "docs\gallery"
New-Item -ItemType Directory -Force $gallery | Out-Null
$profileDir = Join-Path $env:TEMP "palecheck-render-profile"

function Shot([string]$path, [string]$out, [int]$w, [int]$h, [double]$scale = 1) {
    & $browser --headless=new --disable-gpu --hide-scrollbars --no-first-run --user-data-dir="$profileDir" `
        --force-device-scale-factor=$scale --window-size="$w,$h" --virtual-time-budget=20000 `
        --screenshot="$out" "$Base$path" 2>$null | Out-Null
    if (Test-Path $out) { "wrote $out" } else { "FAILED $out" }
}

Shot "/poster" (Join-Path $root "src\PaleCheck.Web\wwwroot\og.png") 1200 630
foreach ($v in "", "audit", "lighting", "honest") {
    $name = if ($v) { "poster-$v.png" } else { "poster-hook.png" }
    Shot "/poster/$v" (Join-Path $gallery $name) 1500 1000
}
foreach ($p in @(@("/", "phone-home.png"), @("/check?sample=6", "phone-result.png"), @("/science", "phone-science.png"), @("/dupescope", "phone-dupescope.png"))) {
    Shot $p[0] (Join-Path $gallery $p[1]) 540 1170 2   # headless Chrome won't go narrower without cropping
}
