param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Unity-Batch.ps1') -Action Build -UnityPath $UnityPath
Write-Host 'Run Builds/Windows/Ashbound.exe to play.'
