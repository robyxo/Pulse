@echo off
REM Script per pubblicare Pulse su GitHub Releases
REM Uso: pubblica.cmd 1.0.1

setlocal enabledelayedexpansion

if "%~1"=="" (
    echo Uso: pubblica.cmd VERSION
    echo Esempio: pubblica.cmd 1.0.1
    exit /b 1
)

set VERSION=%~1
set PACKID=Pulse.Gestionale

echo.
echo ===== Pubblica Pulse v%VERSION% =====
echo.

REM 1. Pulisci le compilazioni precedenti
echo [1/4] Pulizia cartella Releases...
if exist Releases (
    rmdir /s /q Releases
)

REM 2. Compila in Release con la versione
echo [2/4] Compilazione Release...
dotnet publish Pulse/Pulse.csproj ^
    -c Release ^
    -f net9.0-windows10.0.19041.0 ^
    -p:Version=%VERSION% ^
    -p:SelfContained=false ^
    --no-restore

if errorlevel 1 (
    echo Errore nella compilazione!
    exit /b 1
)

REM 3. Crea Setup e pacchetto di aggiornamento con Velopack
echo [3/4] Creazione Setup con Velopack...
vpk pack ^
    --releaseDir Pulse/bin/Release/net9.0-windows10.0.19041.0/win10-x64/publish ^
    --packId %PACKID% ^
    --packVersion %VERSION% ^
    --packTitle "Pulse Gestionale Scuola Ballo" ^
    --packAuthors "Roberto" ^
    --mainExe Pulse.exe ^
    --outputDir Releases

if errorlevel 1 (
    echo Errore nella creazione del pacchetto Velopack!
    exit /b 1
)

REM 4. Pubblica su GitHub Releases
echo [4/4] Pubblicazione su GitHub...
gh release create v%VERSION% ^
    --repo robyxo/Pulse-Releases ^
    --title "v%VERSION%" ^
    --notes "Aggiornamento automatico" ^
    Releases/Pulse.Gestionale-win-Setup.exe ^
    Releases/*.nupkg

if errorlevel 1 (
    echo Errore nella pubblicazione su GitHub!
    echo Verifica che GITHUB_TOKEN sia impostato
    exit /b 1
)

echo.
echo ===== Pubblicazione completata! =====
echo Release v%VERSION% disponibile su:
echo https://github.com/robyxo/Pulse-Releases/releases/tag/v%VERSION%
echo.
