# PowerShell script to download and install JDK 21

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "JDK 21 Installation Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if JDK 21 is already installed
$jdk21Path = "C:\Program Files\Eclipse Adoptium\jdk-21*"
if (Test-Path $jdk21Path) {
    Write-Host "JDK 21 appears to be already installed at:" -ForegroundColor Green
    Get-ChildItem "C:\Program Files\Eclipse Adoptium\" | Where-Object { $_.Name -like "jdk-21*" }
    Write-Host ""
    
    $response = Read-Host "Do you want to reinstall? (y/n)"
    if ($response -ne 'y') {
        Write-Host "Skipping installation." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "Downloading JDK 21 (Eclipse Temurin)..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Yellow
Write-Host ""

# Download URL for Eclipse Temurin JDK 21 LTS (Windows x64)
$jdkUrl = "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B11/OpenJDK21U-jdk_x64_windows_hotspot_21.0.6_11.msi"
$installerPath = "$env:TEMP\temurin-jdk21.msi"

try {
    # Download the installer
    Invoke-WebRequest -Uri $jdkUrl -OutFile $installerPath -UseBasicParsing
    
    Write-Host "Download complete!" -ForegroundColor Green
    Write-Host "Installing JDK 21..." -ForegroundColor Yellow
    Write-Host "Please allow the installer to run with administrator privileges." -ForegroundColor Yellow
    Write-Host ""
    
    # Install with options: install for all users, set JAVA_HOME, add to PATH
    Start-Process msiexec.exe -ArgumentList "/i `"$installerPath`" ADDLOCAL=FeatureMain,FeatureEnvironment,FeatureJarFileRunWith,FeatureJavaHome /quiet" -Wait -NoNewWindow
    
    Write-Host ""
    Write-Host "JDK 21 installation complete!" -ForegroundColor Green
    Write-Host ""
    
    # Clean up
    Remove-Item $installerPath -ErrorAction SilentlyContinue
    
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Close and reopen your command prompt/terminal" -ForegroundColor White
    Write-Host "2. Verify installation: java -version" -ForegroundColor White
    Write-Host "3. Build your Android app: dotnet build -f net9.0-android" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
} catch {
    Write-Host ""
    Write-Host "Error downloading or installing JDK 21:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Please try manual installation from:" -ForegroundColor Yellow
    Write-Host "https://adoptium.net/temurin/releases/?version=21" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
