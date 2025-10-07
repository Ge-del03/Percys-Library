<#!
.SYNOPSIS
  Mata cualquier proceso dotnet.exe que esté ejecutando/hosteando PercysLibrary.dll y procesos PercysLibrary directos.
.DESCRIPTION
  Usa Win32_Process para inspeccionar la línea de comandos, identifica dotnet.exe cuyo CommandLine contiene PercysLibrary.dll
  y los termina. También detiene procesos con nombre PercysLibrary.
#>
[CmdletBinding()]
param(
  [switch]$VerboseMode
)

$ErrorActionPreference = 'SilentlyContinue'

Write-Host "-- Buscando procesos bloqueando PercysLibrary.dll --" -ForegroundColor Cyan

# 1. Matar exe directo (si UseAppHost true en alguna config)
Get-Process PercysLibrary -ErrorAction SilentlyContinue | ForEach-Object {
  try { Stop-Process -Id $_.Id -Force -ErrorAction Stop; Write-Host "Terminado PercysLibrary.exe PID $($_.Id)" -ForegroundColor Green }
  catch { Write-Warning "No se pudo terminar PercysLibrary.exe PID $($_.Id): $_" }
}

# 2. Inspeccionar hosts dotnet
$dotnet = Get-CimInstance Win32_Process -Filter 'Name="dotnet.exe"'
if(!$dotnet){ Write-Host "No hay procesos dotnet activos." -ForegroundColor Yellow; exit 0 }

$targets = $dotnet | Where-Object { $_.CommandLine -match 'PercysLibrary.dll' }
if(!$targets){ Write-Host "No se encontró dotnet.exe con PercysLibrary.dll cargado." -ForegroundColor Yellow; exit 0 }

foreach($p in $targets){
  try {
    Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
    Write-Host "Detenido dotnet host PID $($p.ProcessId) -> $($p.CommandLine)" -ForegroundColor Green
  } catch {
    Write-Warning "Fallo al detener PID $($p.ProcessId): $_"
  }
}

Write-Host "-- Listo. Intenta compilar de nuevo. --" -ForegroundColor Cyan
