$content = Get-Content "c:\Users\O11CE\OneDrive\Desktop\ComicReader\Views\SettingsView.xaml"
$output = [System.Collections.Generic.List[string]]::new()

$seenHeaders = [System.Collections.Generic.HashSet[string]]::new()
$insideTabItem = $false
$currentHeader = ""
$skipUntilNextTabItem = $false
$tabControlStarted = $false
$bracketDepth = 0

foreach ($line in $content) {
    # Antes de TabControl, agregar todo
    if (-not $tabControlStarted -and $line -match '^\s*<TabControl') {
        $tabControlStarted = $true
        $output.Add($line)
        continue
    }
    
    if (-not $tabControlStarted) {
        $output.Add($line)
        continue
    }
    
    # Detectar TabItem
    if ($line -match '^\s*<TabItem Header="([^"]*)"') {
        $header = $Matches[1]
        
        if ($seenHeaders.Contains($header)) {
            $skipUntilNextTabItem = $true
            continue
        } else {
            $seenHeaders.Add($header) | Out-Null
            $skipUntilNextTabItem = $false
            $insideTabItem = $true
            $currentHeader = $header
            $output.Add($line)
            continue
        }
    }
    
    # Si estamos saltando duplicados
    if ($skipUntilNextTabItem) {
        continue
    }
    
    # Detectar fin de TabItem
    if ($line -match '^\s*</TabItem>') {
        $insideTabItem = $false
        $output.Add($line)
        continue
    }
    
    # Detectar fin de TabControl
    if ($line -match '^\s*</TabControl>') {
        $output.Add($line)
        # Agregar el resto del archivo
        $restIndex = $content.IndexOf($line) + 1
        for ($i = $restIndex; $i -lt $content.Length; $i++) {
            $output.Add($content[$i])
        }
        break
    }
    
    # Línea normal
    $output.Add($line)
}

$output | Out-File "c:\Users\O11CE\OneDrive\Desktop\ComicReader\Views\SettingsView_rebuilt.xaml" -Encoding UTF8
Write-Host "Archivo reconstruido. Líneas: $($content.Length) -> $($output.Count)"
