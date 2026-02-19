@echo off
echo ========================================
echo Mobile Workload Setup Script
echo ========================================
echo.
echo This script will:
echo 1. Install Android, iOS, and MacCatalyst workloads
echo 2. Restore the project file to support all mobile platforms
echo.
pause

echo.
echo Installing workloads...
dotnet workload install android ios maccatalyst

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Workload installation failed!
    echo Please make sure you have restarted your computer first.
    pause
    exit /b 1
)

echo.
echo Workloads installed successfully!
echo.
echo Now restoring NuGet packages...
dotnet restore

echo.
echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Your project is now ready for mobile development.
echo.
echo To build for Android: dotnet build -f net9.0-android
echo To build for iOS: dotnet build -f net9.0-ios
echo.
echo You can now open the project in Visual Studio.
echo.
pause
