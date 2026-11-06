# Native document export fallback when the bundled DOCX renderer has no LibreOffice on Windows.
param([string]$Only = '', [string]$ReportFile = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$reports = Join-Path $projectRoot 'Docs\Reports'
$renderRoot = Join-Path $projectRoot 'Docs\Render'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    if ($ReportFile) { $files = Get-Item -LiteralPath (Join-Path $reports $ReportFile) }
    else {
        $files = Get-ChildItem -LiteralPath $reports -Filter '*.docx'
        if ($Only) { $numbers = $Only -split ','; $files = $files | Where-Object { $_.BaseName.Substring($_.BaseName.Length - 2) -in $numbers } }
    }
    foreach ($file in $files) {
        $folder = Join-Path $renderRoot $file.BaseName
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        $document = $word.Documents.Open($file.FullName, $false, $true, $false)
        try {
            $document.Repaginate()
            $output = Join-Path $folder ($file.BaseName + '.pdf')
            $document.ExportAsFixedFormat($output, 17)
            Write-Output ($file.Name + ': ' + $document.ComputeStatistics(2) + ' pages')
        }
        finally { $document.Close(0) }
    }
}
finally {
    $word.Quit(0)
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
