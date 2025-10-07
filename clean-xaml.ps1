$content = Get-Content "c:\Users\O11CE\OneDrive\Desktop\ComicReader\Views\SettingsView.xaml"
$lines = [System.Collections.Generic.List[string]]::new()

$inValidSection = $true
$tabItemCount = 0

for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    
    # Detectar TabItems
    if ($line -match '^\s*<TabItem Header=') {
        $tabItemCount++
        
        # Solo mantener los primeros TabItems únicos
        $headerMatch = [regex]::Match($line, 'Header="([^"]*)"')
        if ($headerMatch.Success) {
            $header = $headerMatch.Groups[1].Value
            
            # Lista de headers válidos en orden
            $validHeaders = @("General", "Apariencia y tema", "Animaciones", "Pantalla completa", "Rendimiento", "Estadísticas")
            $headerIndex = $validHeaders.IndexOf($header)
            
            if ($headerIndex -ge 0 -and $headerIndex -eq ($tabItemCount - 1)) {
                $lines.Add($line)
                continue
            } elseif ($headerIndex -ge 0) {
                # Es un header válido pero duplicado, ignorar hasta el próximo TabItem válido
                continue
            }
        }
    }
    
    # Ignorar líneas corruptas comunes
    if ($line -match '(GradientStop Color=|Border\.Background|LinearGradientBrush)' -and $line -notmatch '^\s*<') {
        continue
    }
    
    # Ignorar fragmentos de botones corruptos
    if ($line -match 'Text="(Superman|Batman|Wonder Woman|Flash)"' -and $i -gt 2000) {
        continue
    }
    
    $lines.Add($line)
}

$lines | Out-File "c:\Users\O11CE\OneDrive\Desktop\ComicReader\Views\SettingsView_cleaned.xaml" -Encoding UTF8
Write-Host "Archivo limpiado. Líneas: $($content.Length) -> $($lines.Count)"
