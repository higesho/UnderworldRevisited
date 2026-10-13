# Reports translation coverage without committing or modifying the original game text.
# Example: powershell -File Tools/TranslationCoverage.ps1 -SourceTsv '.../original-strings.tsv' -OutputMarkdown Docs/TRANSLATION_PROGRESS.md
param(
    [Parameter(Mandatory = $true)][string]$SourceTsv,
    [string]$TranslationTsv = 'Localization/ja.tsv',
    [string]$OutputMarkdown
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $SourceTsv).Path
$translations = (Resolve-Path -LiteralPath $TranslationTsv).Path
$blocks = @{}
$sourceKeys = New-Object 'System.Collections.Generic.HashSet[string]'
$translatedKeys = New-Object 'System.Collections.Generic.HashSet[string]'
$sourceCount = 0
$emptyCount = 0

foreach ($line in [System.IO.File]::ReadLines($source, [System.Text.Encoding]::UTF8)) {
    if ($line.StartsWith('#')) { continue }
    $cells = $line.Split(@([char]9), 3)
    if ($cells.Length -ne 3 -or $cells[0] -notmatch '^\d+$' -or $cells[1] -notmatch '^\d+$') { continue }
    $key = $cells[0] + ':' + $cells[1]
    if (-not $sourceKeys.Add($key)) { throw "Duplicated original key: $key" }
    $sourceCount++
    $id = [int]$cells[0]
    if (-not $blocks.ContainsKey($id)) { $blocks[$id] = [ordered]@{ total = 0; translated = 0 } }
    if ($cells[2].Length -eq 0) { $emptyCount++; continue }
    $blocks[$id].total++
}

foreach ($line in [System.IO.File]::ReadLines($translations, [System.Text.Encoding]::UTF8)) {
    if ($line.StartsWith('#') -or -not $line.Trim()) { continue }
    $cells = $line.Split(@([char]9), 3)
    if ($cells.Length -ne 3) { throw 'Malformed translation row' }
    $key = $cells[0] + ':' + $cells[1]
    if (-not $sourceKeys.Contains($key) -or -not $translatedKeys.Add($key)) { throw "Invalid or duplicate translation key: $key" }
    $blocks[[int]$cells[0]].translated++
}

$validSourceCount = $sourceCount - $emptyCount
$remaining = $validSourceCount - $translatedKeys.Count
$blockCount = $blocks.Count
$report = New-Object System.Collections.Generic.List[string]
$report.Add('# Japanese translation coverage')
$report.Add('')
$report.Add('Generated from locally extracted original strings and Localization/ja.tsv. Original game text is not included in this report.')
$report.Add('')
$report.Add("- Source text requiring translation: **$validSourceCount**")
$report.Add("- Translated: **$($translatedKeys.Count)**")
$report.Add("- Remaining nonempty text: **$remaining**")
$report.Add("- Empty source entries: **$emptyCount**")
$report.Add("- Original string blocks: **$blockCount**")
$report.Add("- Coverage: **$([math]::Round(100 * $translatedKeys.Count / $validSourceCount, 2))%**")
$report.Add('')
$report.Add('| Block | Nonempty | Translated | Remaining |')
$report.Add('|---:|---:|---:|---:|')
foreach ($id in @($blocks.Keys | Sort-Object)) {
    $b = $blocks[$id]
    $report.Add('| ' + $id + ' | ' + $b.total + ' | ' + $b.translated + ' | ' + ($b.total - $b.translated) + ' |')
}

$report | Select-Object -First 10
if ($OutputMarkdown) {
    [System.IO.File]::WriteAllLines([System.IO.Path]::GetFullPath($OutputMarkdown), $report.ToArray(), (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "Saved report: $OutputMarkdown"
}
