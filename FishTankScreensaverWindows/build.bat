@echo off
REM Build and install the Fish Tank Screensaver for Windows
REM Usage: build.bat [--install]
REM Requires: .NET 8 SDK, WebView2 Runtime

setlocal

echo Building Fish Tank Screensaver...
echo.

dotnet publish -c Release -r win-x64 --self-contained false -o publish

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Build failed.
    exit /b 1
)

echo.
echo Build successful!

REM Rename .exe to .scr for Windows screensaver
if exist "publish\FishTankScreensaver.scr" del "publish\FishTankScreensaver.scr"
copy "publish\FishTankScreensaver.exe" "publish\FishTankScreensaver.scr" >nul

echo Created: publish\FishTankScreensaver.scr

if "%1"=="--install" (
    echo.
    echo Installing screensaver...
    copy "publish\FishTankScreensaver.scr" "%SYSTEMROOT%\System32\FishTankScreensaver.scr" >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo.
        echo ERROR: Installation requires administrator privileges.
        echo Right-click this script and select "Run as administrator".
        echo.
        echo Alternatively, you can:
        echo   1. Right-click publish\FishTankScreensaver.scr
        echo   2. Select "Install"
        exit /b 1
    )
    echo.
    echo Installed! Open Settings ^> Personalization ^> Lock screen ^> Screen saver
    echo and select "FishTankScreensaver" from the list.
) else (
    echo.
    echo To install, either:
    echo   1. Run: build.bat --install  (as administrator)
    echo   2. Right-click publish\FishTankScreensaver.scr and select "Install"
    echo   3. Copy publish\FishTankScreensaver.scr to %%SYSTEMROOT%%\System32\
)

endlocal
