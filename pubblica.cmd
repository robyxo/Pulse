@echo off
setlocal

rem ================================================================
rem  PUBBLICA UNA NUOVA VERSIONE DI PULSE
rem
rem  Uso (dalla cartella D:\Progetti\Pulse):
rem      pubblica.cmd 1.0.1
rem
rem  Il numero deve essere sempre PIU' ALTO dell'ultima versione
rem  pubblicata, altrimenti le scuole non vedono l'aggiornamento.
rem
rem  Serve, una volta sola sul PC:
rem    - vpk:           dotnet tool install -g vpk --version 0.0.1298
rem    - GITHUB_TOKEN:  setx GITHUB_TOKEN "github_pat_..."
rem  (istruzioni complete in comandi-pulse.md, nel progetto Claude)
rem ================================================================

set "VERSIONE=%~1"
if "%VERSIONE%"=="" (
    echo.
    echo Uso: pubblica.cmd NUMERO_VERSIONE      esempio: pubblica.cmd 1.0.1
    exit /b 1
)

if "%GITHUB_TOKEN%"=="" (
    echo.
    echo Manca la variabile GITHUB_TOKEN.
    echo Creala con:  setx GITHUB_TOKEN "github_pat_..."   e riapri il terminale.
    exit /b 1
)

where vpk >nul 2>nul
if errorlevel 1 (
    echo.
    echo vpk non e' installato. Installalo con:
    echo     dotnet tool install -g vpk --version 0.0.1298
    exit /b 1
)

set "REPO=https://github.com/robyxo/Pulse-Releases"
set "PACKID=Pulse.Gestionale"
set "RADICE=%~dp0"
set "PUBBLICAZIONE=%RADICE%publish"
set "RILASCI=%RADICE%Releases"

cd /d "%RADICE%"

echo.
echo === 1/4  Compilo Pulse %VERSIONE% ===
if exist "%PUBBLICAZIONE%" rmdir /s /q "%PUBBLICAZIONE%"
rem Compilazione da zero: altrimenti puo' riusare un Pulse.exe vecchio (es. con l'icona precedente)
if exist "Pulse\bin\Release" rmdir /s /q "Pulse\bin\Release"
if exist "Pulse\obj\Release" rmdir /s /q "Pulse\obj\Release"
dotnet publish "Pulse\Pulse.csproj" -c Release -f net9.0-windows10.0.19041.0 -p:ApplicationDisplayVersion=%VERSIONE% -o "%PUBBLICAZIONE%"
if errorlevel 1 goto errore

echo.
echo === 2/4  Scarico le versioni gia' pubblicate (servono per gli aggiornamenti piccoli) ===
rem Alla prima pubblicazione non c'e' niente da scaricare: si prosegue lo stesso.
vpk download github --repoUrl %REPO% --token %GITHUB_TOKEN% --outputDir "%RILASCI%"

echo.
echo === 3/4  Preparo installer e pacchetto di aggiornamento ===
vpk pack --packId %PACKID% --packVersion %VERSIONE% --packDir "%PUBBLICAZIONE%" --mainExe Pulse.exe --packTitle Pulse --icon "%RADICE%Pulse\Resources\AppIcon\pulse.ico" --outputDir "%RILASCI%"
if errorlevel 1 goto errore

echo.
echo === 4/4  Pubblico su GitHub ===
vpk upload github --repoUrl %REPO% --token %GITHUB_TOKEN% --publish --releaseName "Pulse %VERSIONE%" --tag v%VERSIONE% --outputDir "%RILASCI%"
if errorlevel 1 goto errore

echo.
echo ================================================================
echo  FATTO: Pulse %VERSIONE% pubblicato.
echo  Installer per una scuola nuova: %REPO%/releases/latest
echo  Le scuole gia' installate lo trovano in Impostazioni ^> Aggiornamenti.
echo ================================================================
exit /b 0

:errore
echo.
echo *** ERRORE: pubblicazione interrotta. Leggi il messaggio qui sopra. ***
exit /b 1
