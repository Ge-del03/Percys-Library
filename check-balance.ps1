$content = Get-Content "c:\Users\O11CE\OneDrive\Desktop\ComicReader\Views\SettingsView.xaml"
$tabControlCount = 0
$tabItemCount = 0

foreach ($line in $content) {
    if ($line -match '<TabControl') { $tabControlCount++ }
    if ($line -match '</TabControl>') { $tabControlCount-- }
    if ($line -match '<TabItem') { $tabItemCount++ }
    if ($line -match '</TabItem>') { $tabItemCount-- }
}

Write-Host "Balance final:"
Write-Host "TabControl: $tabControlCount (debería ser 0)"
Write-Host "TabItem: $tabItemCount (debería ser 0)"

# Buscar elementos comunes sin cerrar
$stackPanelCount = 0
$borderCount = 0
$scrollViewerCount = 0

foreach ($line in $content) {
    if ($line -match '<StackPanel[^>]*>') { $stackPanelCount++ }
    if ($line -match '</StackPanel>') { $stackPanelCount-- }
    if ($line -match '<Border[^>]*>') { $borderCount++ }
    if ($line -match '</Border>') { $borderCount-- }
    if ($line -match '<ScrollViewer[^>]*>') { $scrollViewerCount++ }
    if ($line -match '</ScrollViewer>') { $scrollViewerCount-- }
}

Write-Host "Otros balances:"
Write-Host "StackPanel: $stackPanelCount"
Write-Host "Border: $borderCount" 
Write-Host "ScrollViewer: $scrollViewerCount"
