# Reports translation coverage without committing or modifying the original game text.
# Example: powershell -File Tools/TranslationCoverage.ps1 -SourceTsv '.../original-strings.tsv' -OutputMarkdown Docs/TRANSLATION_PROGRESS.md
param(
    [Parameter(Mandatory = $true)][string]$SourceTsv,
    [string]$TranslationTsv = 'Localization/ja.tsv',
    [string]$PreserveTsv = 'Localization/ja-preserve.tsv',
    [string]$OutputMarkdown
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $SourceTsv).Path
$translations = (Resolve-Path -LiteralPath $TranslationTsv).Path
$preserve = (Resolve-Path -LiteralPath $PreserveTsv).Path
$blocks = @{}
$sourceKeys = New-Object 'System.Collections.Generic.HashSet[string]'
$sourceTextKeys = New-Object 'System.Collections.Generic.HashSet[string]'
$translatedKeys = New-Object 'System.Collections.Generic.HashSet[string]'
$preservedKeys = New-Object 'System.Collections.Generic.HashSet[string]'
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
    if (-not $blocks.ContainsKey($id)) { $blocks[$id] = [ordered]@{ total = 0; translated = 0; preserved = 0 } }
    if ($cells[2].Length -eq 0) { $emptyCount++; continue }
    $blocks[$id].total++
    [void]$sourceTextKeys.Add($key)
}

foreach ($line in [System.IO.File]::ReadLines($translations, [System.Text.Encoding]::UTF8)) {
    if ($line.StartsWith('#') -or -not $line.Trim()) { continue }
    $cells = $line.Split(@([char]9), 3)
    if ($cells.Length -ne 3) { throw 'Malformed translation row' }
    $key = $cells[0] + ':' + $cells[1]
    if (-not $sourceTextKeys.Contains($key) -or -not $translatedKeys.Add($key)) { throw "Invalid or duplicate translation key: $key" }
    $blocks[[int]$cells[0]].translated++
}

foreach ($line in [System.IO.File]::ReadLines($preserve, [System.Text.Encoding]::UTF8)) {
    if ($line.StartsWith('#') -or -not $line.Trim()) { continue }
    $cells = $line.Split(@([char]9), 3)
    if ($cells.Length -ne 3 -or $cells[0] -notmatch '^\d+$' -or
        $cells[1] -notmatch '^\d+$' -or [string]::IsNullOrWhiteSpace($cells[2])) {
        throw 'Malformed preservation row'
    }
    $key = $cells[0] + ':' + $cells[1]
    if (-not $sourceTextKeys.Contains($key) -or $translatedKeys.Contains($key) -or
        -not $preservedKeys.Add($key)) { throw "Invalid, translated or duplicate preservation key: $key" }
    $blocks[[int]$cells[0]].preserved++
}

$validSourceCount = $sourceCount - $emptyCount
$remaining = $validSourceCount - $translatedKeys.Count - $preservedKeys.Count
$blockCount = $blocks.Count
$report = New-Object System.Collections.Generic.List[string]
$report.Add('# Japanese translation coverage')
$report.Add('')
$report.Add('Generated from locally extracted original strings, Localization/ja.tsv and Localization/ja-preserve.tsv. Original game text is not included in this report.')
$report.Add('')
$report.Add("- Nonempty source strings: **$validSourceCount**")
$report.Add("- Translated: **$($translatedKeys.Count)**")
$report.Add("- Intentionally preserved original strings: **$($preservedKeys.Count)**")
$report.Add("- Remaining requiring translation: **$remaining**")
$report.Add("- Empty source entries: **$emptyCount**")
$report.Add("- Original string blocks: **$blockCount**")
$report.Add("- Translation coverage: **$([math]::Round(100 * $translatedKeys.Count / $validSourceCount, 2))%**")
$report.Add("- Reviewed coverage (translated plus preserved): **$([math]::Round(100 * ($translatedKeys.Count + $preservedKeys.Count) / $validSourceCount, 2))%**")
$report.Add('')
$report.Add('| Block | Nonempty | Translated | Preserved | Remaining |')
$report.Add('|---:|---:|---:|---:|---:|')
foreach ($id in @($blocks.Keys | Sort-Object)) {
    $b = $blocks[$id]
    $report.Add('| ' + $id + ' | ' + $b.total + ' | ' + $b.translated + ' | ' + $b.preserved + ' | ' + ($b.total - $b.translated - $b.preserved) + ' |')
}

$report | Select-Object -First 10
if ($OutputMarkdown) {
    [System.IO.File]::WriteAllLines([System.IO.Path]::GetFullPath($OutputMarkdown), $report.ToArray(), (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "Saved report: $OutputMarkdown"
}
