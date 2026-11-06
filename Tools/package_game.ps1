$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $projectRoot 'Build\Neonmoves\AI'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($folder in @('runtime', 'models')) {
    $source = Join-Path $projectRoot ('AI\' + $folder)
    Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
}
foreach ($name in @('bridge.py', 'pose_math.py', 'camera_io.py', 'requirements.txt')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('AI\' + $name)) -Destination $destination -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\Resources\PoseCatalog.json') -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'ThirdParty') -Destination (Join-Path $projectRoot 'Build\Neonmoves') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Docs\Controls.txt') -Destination (Join-Path $projectRoot 'Build\Neonmoves\How-to-play.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Docs\Poses.txt') -Destination (Join-Path $projectRoot 'Build\Neonmoves\Poses.txt') -Force
Write-Output 'Portable AI runtime packaged beside Neonmoves.exe.'
