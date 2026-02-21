# Push to GitHub Script
# This script will prepare your code and push to GitHub

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "E-z MicLink - Push to GitHub" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Check if git is initialized
if (-not (Test-Path ".git")) {
    Write-Host "❌ Not a git repository!" -ForegroundColor Red
    Write-Host "Run: git init" -ForegroundColor Yellow
    exit 1
}

# Stage all files
Write-Host "📦 Staging all files..." -ForegroundColor Yellow
git add .

# Show what will be committed
Write-Host ""
Write-Host "📋 Files to be committed:" -ForegroundColor Green
git status --short

Write-Host ""
$confirm = Read-Host "Continue with commit? (y/n)"

if ($confirm -ne 'y') {
    Write-Host "❌ Aborted" -ForegroundColor Red
    exit 0
}

# Commit with detailed message
Write-Host ""
Write-Host "💾 Creating commit..." -ForegroundColor Yellow

$commitMessage = @"
Initial commit: E-z MicLink Voice Changer with Discovery Automation

## Features Implemented

### Voice Effects & DSP
- Professional voice effects (Helium, Deep, Robot, Megaphone, etc.)
- SimplePitchShifter with formant preservation
- Complete DSP chain (Noise Gate, EQ, Compression, Limiting)
- 10+ voice presets with proper audio processing

### Settings & UI
- Premium glass-morphism settings page
- Dark gradient theme with neon blue accents
- Audio & Microphone settings (6 controls)
- Bluetooth & Devices settings (4 controls)
- App Preferences settings (4 controls)
- User authentication system

### Authentication
- Multi-provider login (Phone, Google, Apple, Guest)
- Session persistence across app restarts
- Settings page integration
- 6 authentication sanity tests

### Voice Discovery Automation 🤖
- /discover-voice-effects skill
- AI agent researches trending effects
- Automated every 3 days via GitHub Actions
- Email delivery to yahalom.assets@gmail.com
- Complete DSP specs for discovered effects

### Skills System
- /create-voice-effect - Create new effects
- /equalizer - Design custom EQ
- /audio-preset - Manage effect presets
- /discover-voice-effects - Find trending effects

### Testing
- 16 automated sanity tests
- 33 manual integration tests
- Complete test documentation
- CI/CD integration ready

### Documentation
- Comprehensive setup guides
- Email automation instructions
- Skills documentation
- Test coverage reports
- Bug fix summaries

## Technical Stack
- .NET MAUI 9.0
- C# audio DSP
- Real-time processing optimized
- Android/iOS platform support
- GitHub Actions automation

## Ready For
✅ Email automation setup (10 min)
✅ Voice discovery testing
✅ Implementation of discovered effects
✅ Continuous feature innovation

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
"@

git commit -m $commitMessage

Write-Host "✅ Commit created!" -ForegroundColor Green

# Check for remote
Write-Host ""
Write-Host "🔍 Checking for GitHub remote..." -ForegroundColor Yellow

$remote = git remote -v | Select-String "origin"

if (-not $remote) {
    Write-Host ""
    Write-Host "⚠️  No GitHub remote configured!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Create GitHub repository at: https://github.com/new" -ForegroundColor White
    Write-Host "2. Name it: BluetoothMicrophoneApp (or ez-miclink)" -ForegroundColor White
    Write-Host "3. Do NOT initialize with README/gitignore/license" -ForegroundColor White
    Write-Host "4. Copy the repository URL" -ForegroundColor White
    Write-Host "5. Run these commands:" -ForegroundColor White
    Write-Host ""
    Write-Host "   git remote add origin https://github.com/Ezbyname/BluetoothMicrophoneApp.git" -ForegroundColor Yellow
    Write-Host "   git branch -M main" -ForegroundColor Yellow
    Write-Host "   git push -u origin main" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📖 See GITHUB_PROJECT_SETUP.md for detailed instructions" -ForegroundColor Cyan
} else {
    Write-Host "✅ Remote configured: $remote" -ForegroundColor Green
    Write-Host ""
    $push = Read-Host "Push to GitHub now? (y/n)"

    if ($push -eq 'y') {
        Write-Host ""
        Write-Host "📤 Pushing to GitHub..." -ForegroundColor Yellow

        # Rename branch to main if needed
        $currentBranch = git branch --show-current
        if ($currentBranch -ne "main") {
            Write-Host "🔄 Renaming branch to 'main'..." -ForegroundColor Yellow
            git branch -M main
        }

        # Push
        git push -u origin main

        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✅ Successfully pushed to GitHub!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Next steps:" -ForegroundColor Cyan
            Write-Host "1. Visit your repository on GitHub" -ForegroundColor White
            Write-Host "2. Create issues from GITHUB_PROJECT_SETUP.md" -ForegroundColor White
            Write-Host "3. Link issues to project: https://github.com/users/Ezbyname/projects/1" -ForegroundColor White
            Write-Host "4. Setup email automation (Issue #1)" -ForegroundColor White
        } else {
            Write-Host ""
            Write-Host "❌ Push failed!" -ForegroundColor Red
            Write-Host "Check error messages above" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "✨ Done!" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
