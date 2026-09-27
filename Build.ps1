param(
    [Parameter(Mandatory = $true)]
    [string]$GameDirectory
)

$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $GameDirectory).Path
$managed = Join-Path $game 'v8so_Data\Managed'
$bepInExCore = Join-Path $game 'BepInEx\core'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $PSScriptRoot 'src\VigilanteCustomMusic.cs'
$release = Join-Path $PSScriptRoot 'release'
$output = Join-Path $release 'VigilanteCustomMusic.dll'

$references = @(
    (Join-Path $bepInExCore 'BepInEx.dll'),
    (Join-Path $bepInExCore '0Harmony.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.AudioModule.dll'),
    (Join-Path $managed 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managed 'UnityEngine.UnityWebRequestModule.dll'),
    (Join-Path $managed 'UnityEngine.UnityWebRequestAudioModule.dll')
)

foreach ($path in @($compiler, $source) + $references) {
    if (!(Test-Path -LiteralPath $path)) { throw "Required file not found: $path" }
}

New-Item -ItemType Directory -Force -Path $release | Out-Null
$arguments = @('/nologo', '/target:library', '/optimize+', "/out:$output")
$arguments += $references | ForEach-Object { "/reference:$_" }
$arguments += $source
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Compiler exited with code $LASTEXITCODE." }
Write-Host "Built: $output"
