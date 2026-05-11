@echo off
REM SETT Publish Script
REM Run this from the scripts folder

setlocal enabledelayedexpansion

echo ========================================
echo SETT Deployment Build Script
echo ========================================
echo.

REM Get the script's directory
set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%.."

REM Check prerequisites
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK not found
    echo Please install .NET SDK 8 and 9
    pause
    exit /b 1
)

where npm >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Node.js not found
    echo Please install Node.js
    pause
    exit /b 1
)

echo [OK] Prerequisites check passed
echo.

REM Step 1: Build Frontend
echo [1/4] Building Frontend (Next.js static export)...
cd /d "%PROJECT_ROOT%\frontend"
if errorlevel 1 (
    echo ERROR: Could not change to frontend directory
    pause
    exit /b 1
)

echo Running: npm run build
call npm run build
if errorlevel 1 (
    echo ERROR: Frontend build failed
    pause
    exit /b 1
)
echo [OK] Frontend built
echo.

REM Step 2: Copy frontend to API wwwroot
echo [2/4] Copying frontend to API wwwroot...
if not exist "%PROJECT_ROOT%\backend\settAPI\wwwroot" (
    mkdir "%PROJECT_ROOT%\backend\settAPI\wwwroot"
)

echo Running: xcopy "%PROJECT_ROOT%\frontend\out\*" "%PROJECT_ROOT%\backend\settAPI\wwwroot\"
xcopy /E /Y /I /Q "%PROJECT_ROOT%\frontend\out\*" "%PROJECT_ROOT%\backend\settAPI\wwwroot\"
if errorlevel 1 (
    echo WARNING: Could not copy frontend files (may not exist yet)
)
echo [OK] Frontend copied to API
echo.

REM Step 3: Publish API
echo [3/4] Publishing SETT API...
cd /d "%PROJECT_ROOT%\backend\settAPI"
echo Running: dotnet publish -c Release --self-contained true -r win-x64 -o "%PROJECT_ROOT%\publish\api"
dotnet publish -c Release --self-contained true -r win-x64 -o "%PROJECT_ROOT%\publish\api"
if errorlevel 1 (
    echo ERROR: API publish failed
    pause
    exit /b 1
)
echo [OK] API published
echo.

REM Step 4: Publish Agent
echo [4/4] Publishing SETT Agent...
cd /d "%PROJECT_ROOT%\desktop-service\settAGENT"
echo Running: dotnet publish -c Release --self-contained true -r win-x64 -o "%PROJECT_ROOT%\publish\agent"
dotnet publish -c Release --self-contained true -r win-x64 -o "%PROJECT_ROOT%\publish\agent"
if errorlevel 1 (
    echo ERROR: Agent publish failed
    pause
    exit /b 1
)
echo [OK] Agent published
echo.

echo ========================================
echo Build Complete!
echo ========================================
echo Output locations:
echo   - API:     %PROJECT_ROOT%\publish\api\
echo   - Agent:   %PROJECT_ROOT%\publish\agent\
echo.
echo Next steps:
echo   1. Open installer\sett.iss in InnoSetup
echo   2. Compile to create SETT-Setup.exe
echo.
pause