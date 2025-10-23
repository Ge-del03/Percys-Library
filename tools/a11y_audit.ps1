# A11y audit script: scans Styles/*.xaml for color tokens and reports basic contrast with white/black
param()

Write-Host "Running lightweight A11y audit (colors)"
$files = Get-ChildItem -Path "$PSScriptRoot\..\Styles" -Filter *.xaml -Recurse
$colors = @{}
foreach ($f in $files) {
    $text = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($text, '#[0-9A-Fa-f]{6,8}')
    foreach ($m in $matches) {
        $c = $m.Value.ToUpper()
        if (-not $colors.ContainsKey($c)) { $colors[$c] = @() }
        $colors[$c] += $f.Name
    }
}

function Luminance($r, $g, $b) {
    $rs = $r/255.0; $gs = $g/255.0; $bs = $b/255.0
    $f = { param($c) if ($c -le 0.03928) { $c/12.92 } else { [math]::Pow((($c+0.055)/1.055),2.4) } }
    return 0.2126*$f.Invoke($rs)+0.7152*$f.Invoke($gs)+0.0722*$f.Invoke($bs)
}

function Contrast($hex) {
    $hex = $hex.TrimStart('#')
    $r = [Convert]::ToInt32($hex.Substring(0,2),16)
    $g = [Convert]::ToInt32($hex.Substring(2,2),16)
    $b = [Convert]::ToInt32($hex.Substring(4,2),16)
    $L = Luminance $r $g $b
    $Lw = 1.0
    $Lb = 0.0
    $crw = ([math]::Round((($Lw+0.05)/($L+0.05)),2))
    $crb = ([math]::Round((($L+0.05)/($Lb+0.05)),2))
    return @{ 'againstWhite'=$crw; 'againstBlack'=$crb }
}

$report = @()
foreach ($k in $colors.Keys) {
    $contrast = Contrast $k
    $report += [PSCustomObject]@{ Color=$k; AgainstWhite=$contrast.againstWhite; AgainstBlack=$contrast.againstBlack; Files=($colors[$k] -join ', ') }
}

$report | Sort-Object AgainstWhite -Descending | Format-Table -AutoSize
$report | Out-File -FilePath "$PSScriptRoot\..\Docs\A11y_Report_raw.txt"
Write-Host "Report written to Docs/A11y_Report_raw.txt"
