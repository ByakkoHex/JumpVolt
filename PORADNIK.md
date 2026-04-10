# 📖 JumpVolt — Kompletny Poradnik

---

## 🔨 Jak zbudować plik .EXE (instalacja na innym komputerze)

### Szybka metoda — samodzielny .exe

Otwórz **PowerShell** w folderze projektu i wpisz:

```powershell
cd "Z:\Aplikacja dla taty\SklepMotoryzacyjny"

dotnet publish SklepMotoryzacyjny/SklepMotoryzacyjny.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o ./publish
```

Wynik: folder `publish/` z jednym plikiem `SklepMotoryzacyjny.exe` (~80 MB).
Zawiera w sobie .NET runtime — **nie wymaga instalacji .NET na docelowym PC**.

Możesz go skopiować na pendrive i uruchomić na dowolnym Windows 10/11 64-bit.

### Metoda z instalatorem (opcjonalna)

Jeśli chcesz ładny instalator (.msi / setup.exe):

1. Zainstaluj **Inno Setup** (darmowy): https://jrsoftware.org/isdl.php
2. Stwórz plik `installer.iss`:

```ini
[Setup]
AppName=JumpVolt
AppVersion=1.0
DefaultDirName={autopf}\JumpVolt
DefaultGroupName=JumpVolt
OutputDir=installer_output
OutputBaseFilename=JumpVolt_Setup
SetupIconFile=SklepMotoryzacyjny\Resources\app.ico
Compression=lzma2
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\JumpVolt"; Filename: "{app}\SklepMotoryzacyjny.exe"
Name: "{commondesktop}\JumpVolt"; Filename: "{app}\SklepMotoryzacyjny.exe"
```

3. Uruchom Inno Setup → otwórz ten plik → Compile
4. Dostaniesz `JumpVolt_Setup.exe` — klasyczny instalator Windows

---

## 🗄️ Serwer — gdzie przechowywać dane

### Opcja 1: Lokalna baza (obecne rozwiązanie)
- Baza SQLite w `%LOCALAPPDATA%\JumpVolt\jumpvolt.db`
- Zdjęcia w `%LOCALAPPDATA%\JumpVolt\Images\`
- **Zalety:** działa offline, zero konfiguracji
- **Wady:** dane na jednym komputerze

**Backup:** kopiuj regularnie folder `%LOCALAPPDATA%\JumpVolt\` na pendrive/dysk zewnętrzny.

### Opcja 2: Folder sieciowy (najprostszy "serwer")
Jeśli masz drugi komputer lub NAS w sklepie:
1. Udostępnij folder sieciowy, np. `\\NAS\JumpVolt\`
2. W kodzie zmień ścieżkę bazy na folder sieciowy
3. Oba komputery korzystają z tych samych danych

**Uwaga:** SQLite nie obsługuje wielu użytkowników jednocześnie.

### Opcja 3: Prawdziwy serwer (PostgreSQL/MySQL)
Jeśli w przyszłości będziesz potrzebować:
- Wielu stanowisk kasowych jednocześnie
- Dostępu zdalnego (np. z domu)
- Automatycznych backupów

To wymagałoby przerobienia `DatabaseService.cs` na PostgreSQL.
Powiedz mi jeśli będziesz tego potrzebować — przerobię.

### Rekomendacja dla sklepu:
**Opcja 1 + automatyczny backup** — najprostsza, niezawodna. 
Mogę dodać do aplikacji przycisk "Zrób backup" który skopiuje bazę na pendrive.

---

## 🤖 Claude Code — Jak edytować projekt razem ze mną

### Co to jest?
Claude Code to narzędzie konsolowe. Uruchamiasz je w folderze projektu,
mówisz co zmienić, a ja sam edytuję pliki. Rider od razu widzi zmiany.

### Instalacja (5 minut)

**1. Zainstaluj Node.js** (jeśli nie masz):
   - https://nodejs.org → pobierz LTS → zainstaluj

**2. Zainstaluj Claude Code:**
```powershell
npm install -g @anthropic-ai/claude-code
```

**3. Wejdź do folderu projektu:**
```powershell
cd "Z:\Aplikacja dla taty\SklepMotoryzacyjny"
```

**4. Uruchom:**
```powershell
claude
```

Przy pierwszym uruchomieniu poprosi o klucz API z https://console.anthropic.com/settings/keys

### ⚠️ Ważne: Claude Code to ODDZIELNA rozmowa!

Claude Code **nie łączy się z tym czatem**. To osobna sesja — jak nowy pokój.
Ale Claude Code widzi Twoje pliki i może je edytować, więc wystarczy powiedzieć:

```
> Kontynuuję pracę nad JumpVolt. Zrób mi X.
```

Claude Code przeczyta pliki projektu i będzie wiedział co robić.

**Jeśli chcesz kontynuować dokładnie tę rozmowę:**
- Po prostu wróć na https://claude.ai i otwórz ten czat
- Wrzuć mi pliki lub screeny i powiedz co zmienić
- Dam Ci gotowe pliki do podmianki

### Przykłady poleceń w Claude Code:

```
> Dodaj rabat procentowy do ekranu sprzedaży
> Zmień kolor przycisków na jaśniejszy
> Dodaj eksport do CSV w historii
> Napraw błąd: [wklej treść błędu]
> Dodaj nową kategorię "Filtry" z polami: typ, średnica
```

### Koszt
Claude Code używa API (płatne). Typowa sesja: $0.50–3.00.
Alternatywa: dalej gadamy tutaj na czacie za darmo (plan Pro).

---

## 💡 Szybki poradnik: Git (cofanie zmian)

Zanim zaczniesz edytować z Claude Code, warto mieć Git:

```powershell
cd "Z:\Aplikacja dla taty\SklepMotoryzacyjny"
git init
git add .
git commit -m "Wersja bazowa JumpVolt"
```

Teraz jak Claude coś zepsuje:
```powershell
git diff                    # co się zmieniło
git checkout -- .           # cofnij WSZYSTKO do ostatniego commita
git checkout -- App.xaml    # cofnij jeden plik
```

Po udanej zmianie:
```powershell
git add .
git commit -m "Dodano rabaty"
```
