param([string]$UnityEditor = 'E:\UN\2022.3.35f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
#两个工程依次更新同一清单，不并发写入导出结果。
foreach ($project in @('E:\mygame\chezhoulibShader', 'E:\mygame\PhoenixRace')) {
    if (Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -like "*$project*" }) {
        throw "Unity 工程正在使用：$project"
    }
    Copy-Item -LiteralPath "$PSScriptRoot\PhoenixRimUnityExport.cs" -Destination "$project\Assets\Editor\PhoenixRimUnityExport.cs" -Force
    $log = Join-Path $PSScriptRoot "..\Build\rimunity-$([IO.Path]::GetFileName($project)).log"
    $process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',$project,
        '-executeMethod','PhoenixRimUnityExport.Export','-logFile',$log) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -Pattern 'PHOENIX_RIMUNITY_EXPORT_OK' -Quiet)) {
        throw "RimUnity 导出失败：$log"
    }
}
