param(
    [string]$Configuration = "Debug"
)

# Hacer el script robusto ante errores no críticos en limpieza
$ErrorActionPreference = "Continue"

# Directorio raíz del workspace (padre de la carpeta 'tools')
$workspace = Resolve-Path (Join-Path $PSScriptRoot "..")
Write-Host "Workspace: $workspace"

# Intentar cerrar cualquier instancia en ejecución de PercysLibrary
Write-Host "Terminando procesos PercysLibrary si están en ejecución..."
try {
    Get-Process PercysLibrary -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
} catch {}

# Esperar a que el EXE no esté bloqueado
$exePath = Join-Path $workspace "bin/Debug/net6.0-windows/PercysLibrary.exe"
if (Test-Path $exePath) {
    Write-Host "Esperando a que se libere: $exePath"
    for ($i = 0; $i -lt 50; $i++) {
        try {
            $fs = [System.IO.File]::Open($exePath, 'Open', 'ReadWrite', 'None')
            $fs.Close()
            Write-Host "Archivo liberado."
            break
        } catch {
            Start-Sleep -Milliseconds 200
        }
    }
}

# Limpiar bin y obj en todo el árbol
Write-Host "Eliminando carpetas bin/obj..."
Get-ChildItem -Path $workspace -Directory -Include bin,obj -Recurse -Force -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Limpiar posibles restos en %LOCALAPPDATA%\PercysLibrary (si se usó salida redirigida)
$localOut = Join-Path $env:LOCALAPPDATA "PercysLibrary"
if (Test-Path $localOut) {
    Write-Host "Eliminando salida local: $localOut"
    try { Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $localOut } catch {}
}

# Apagar servidores de compilación para evitar reutilización de caché/rutas antiguas
Write-Host "dotnet build-server shutdown..."
dotnet build-server shutdown | Out-Null

# Limpiar y compilar solución
Write-Host "dotnet clean..."
dotnet clean (Join-Path $workspace "ComicReader.sln") -c $Configuration

Write-Host "dotnet build..."
dotnet build (Join-Path $workspace "ComicReader.sln") -c $Configuration
