param(
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root "src\WeeklyScreenPen\WeeklyScreenPen.csproj"
$out = Join-Path $root "publish"

$dotnetArgs = @(
    'publish', $proj,
    '-c', 'Release',
    '-o', $out,
    '-r', 'win-x64',
    '-p:PublishSingleFile=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    '--nologo'
)

if ($SelfContained) {
    $dotnetArgs += @('--self-contained', 'true', '-p:IncludeNativeLibrariesForSelfExtract=true')
}
else {
    $dotnetArgs += @('--self-contained', 'false')
}

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) { throw "publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $out 'WeeklyScreenPen.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "exe not found: $exe" }

$sizeMB = [Math]::Round((Get-Item -LiteralPath $exe).Length / 1MB, 2)
Write-Host ""
Write-Host "Ready: $exe ($sizeMB MB)"
Write-Host "Settings are stored next to the exe (settings.json)."
