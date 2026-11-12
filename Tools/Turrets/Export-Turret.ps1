param(
    [ValidateSet('Inspect', 'Export')][string]$Method = 'Export',
    [string]$UnityEditor = 'E:\UN\2022.3.35f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = 'E:\mygame\PhoenixRace'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if (Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -like "*$project*" }) {
    throw "Unity 工程正在使用：$project"
}
New-Item -ItemType Directory -Path "$project\Assets\Models\PhoenixTripleTurret", "$repository\Build" -Force | Out-Null
Copy-Item -LiteralPath "$repository\ArtSource\Turrets\炮塔_骨骼.fbx" -Destination "$project\Assets\Models\PhoenixTripleTurret\炮塔_骨骼.fbx" -Force
Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'PhoenixTurret*.cs' -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path "$project\Assets\Editor" $_.Name) -Force
}
$log = Join-Path $repository "Build\turret-$Method.log"
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-quit', '-projectPath', $project,
    '-executeMethod', "PhoenixTurretExport.$Method", '-logFile', $log) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -Pattern "PHOENIX_TURRET_$($Method.ToUpperInvariant())_OK" -Quiet)) {
    throw "炮塔资源处理失败：$log"
}
Write-Output "炮塔资源处理完成：$log"
