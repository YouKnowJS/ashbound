param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Unity-Batch.ps1') -Action Content -UnityPath $UnityPath
& (Join-Path $PSScriptRoot 'Unity-Batch.ps1') -Action EditMode -UnityPath $UnityPath
& (Join-Path $PSScriptRoot 'Unity-Batch.ps1') -Action PlayMode -UnityPath $UnityPath
Write-Host 'Prototype verification passed.'
