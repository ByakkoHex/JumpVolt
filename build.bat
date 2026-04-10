@echo off
echo ==========================================
echo  JumpVolt - Kompilacja
echo ==========================================
echo.

REM Sprawdz czy .NET SDK jest zainstalowany
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo BLAD: .NET SDK nie jest zainstalowany!
    echo Pobierz ze strony: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/3] Przywracanie pakietow NuGet...
dotnet restore SklepMotoryzacyjny.sln
if %ERRORLEVEL% NEQ 0 (
    echo BLAD: Nie udalo sie przywrocic pakietow!
    pause
    exit /b 1
)

echo.
echo [2/3] Kompilacja projektu...
dotnet build SklepMotoryzacyjny.sln --configuration Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo BLAD: Kompilacja nie powiodla sie!
    pause
    exit /b 1
)

echo.
echo [3/3] Publikacja jako pojedynczy plik .exe...
dotnet publish SklepMotoryzacyjny/SklepMotoryzacyjny.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o ./publish

if %ERRORLEVEL% NEQ 0 (
    echo BLAD: Publikacja nie powiodla sie!
    pause
    exit /b 1
)

echo.
echo ==========================================
echo  SUKCES! Plik .exe zostal utworzony w:
echo  .\publish\SklepMotoryzacyjny.exe
echo ==========================================
echo.
echo Mozesz teraz skopiowac plik .exe na docelowy komputer.
echo.
pause
