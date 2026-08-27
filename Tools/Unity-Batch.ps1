param(
    [ValidateSet('Content', 'EditMode', 'PlayMode', 'Build')][string]$Action = 'Content',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$taskProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity Editor not found: $UnityPath. Pass -UnityPath with an installed Unity 6 Editor." }
$taskResults = Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Force -Path $taskResults | Out-Null
$taskLog = Join-Path $taskResults ($Action.ToLowerInvariant() + '.log')
$taskArgs = '-batchmode -nographics -projectPath "' + $taskProject + '" -logFile "' + $taskLog + '"'
switch ($Action) {
    'Content' { $taskArgs += ' -quit -executeMethod Ashbound.Editor.PrototypeContentBuilder.CreateContent' }
    'Build' { $taskArgs += ' -quit -executeMethod Ashbound.Editor.PrototypeContentBuilder.BuildWindows' }
    default {
        $taskXml = Join-Path $taskResults ($Action.ToLowerInvariant() + '.xml')
        if (Test-Path -LiteralPath $taskXml) { Remove-Item -LiteralPath $taskXml }
        $taskArgs += ' -runTests -testPlatform ' + $Action + ' -testResults "' + $taskXml + '"'
    }
}
Write-Host "Unity $Action — log: $taskLog"
$taskProcess = Start-Process -FilePath $UnityPath -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskDeadline = [DateTime]::UtcNow.AddMinutes(15)
while (-not $taskProcess.WaitForExit(1000)) {
    if ([DateTime]::UtcNow -gt $taskDeadline) {
        Stop-Process -Id $taskProcess.Id
        throw "Unity $Action exceeded 15 minutes. Inspect $taskLog"
    }
}
if ($taskProcess.ExitCode -ne 0) {
    Get-Content -LiteralPath $taskLog -Tail 35
    throw "Unity $Action exited with code $($taskProcess.ExitCode). Close any Editor using this project and check licensing/compiler errors in the log."
}
if ($Action -eq 'EditMode' -or $Action -eq 'PlayMode') {
    if (-not (Test-Path -LiteralPath $taskXml)) { throw "Unity returned no test XML: $taskXml" }
    [xml]$taskReport = Get-Content -LiteralPath $taskXml -Raw
    $taskRun = $taskReport.'test-run'
    Write-Host "$Action — $($taskRun.passed) passed, $($taskRun.failed) failed, $($taskRun.skipped) skipped"
    if ($taskRun.result -ne 'Passed' -or [int]$taskRun.total -eq 0) { throw "Tests did not pass. See $taskXml" }
}
