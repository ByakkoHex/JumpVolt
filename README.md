# JumpVolt

System sprzedażowo-magazynowy dla sklepu motoryzacyjnego. Aplikacja desktopowa WPF (.NET 8) z integracją kasy fiskalnej Novitus Nano Online, wystawianiem faktur PDF, eksportem do Excela i automatyczną aktualizacją.

---

## Funkcje

- **Sprzedaż** — koszyk, skaner kodów kreskowych, płatność gotówką / kartą / przelewem, druk paragonu fiskalnego, obsługa usług (np. serwis akumulatora)
- **Magazyn** — CRUD produktów ze zdjęciami, kategorie z dedykowanymi polami technicznymi (akumulatory, oleje, żarówki itd.), zarządzanie markami
- **Historia sprzedaży** — filtrowanie po datach, podgląd paragonów, eksport do Excela, raporty fiskalne X i Z
- **Zwroty** — wybór paragonu, zaznaczanie pozycji, częściowe ilości, automatyczne przywrócenie stanu magazynu
- **Faktury** — generowanie PDF, wysyłka e-mailem
- **Ustawienia** — dane firmy, konfiguracja kasy fiskalnej (COM / TCP/IP), terminal kartowy, harmonogram kopii zapasowych, tryb sieciowej bazy danych
- **Diagnostyka** — test połączenia z kasą, wydruk testowy niefiskalny, test szuflady, sprawdzanie i pobieranie aktualizacji
- **Autoaktualizacja** — sprawdzanie nowej wersji z GitHub Releases, pobieranie instalatora w tle

---

## Wymagania

| Wymaganie | Wersja |
|-----------|--------|
| System | Windows 10 / 11 (64-bit) |
| .NET | 8.0 (wbudowany w instalator) |
| Kasa fiskalna | Novitus Nano Online (opcjonalnie) |

---

## Instalacja

Pobierz najnowszy instalator ze strony [Releases](../../releases/latest) i uruchom plik `JumpVolt-Setup-x.x.x.exe`.

Baza danych SQLite tworzona jest automatycznie przy pierwszym uruchomieniu:
```
%LOCALAPPDATA%\JumpVolt\jumpvolt.db
```

---

## Budowanie ze źródeł

```powershell
# Klonowanie
git clone https://github.com/ByakkoHex/JumpVolt.git
cd JumpVolt

# Uruchomienie (tryb deweloperski)
dotnet run --project SklepMotoryzacyjny/SklepMotoryzacyjny.csproj

# Publikacja jako samodzielny .exe
dotnet publish SklepMotoryzacyjny/SklepMotoryzacyjny.csproj ^
  -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -o ./publish
```

---

## Kategorie produktów

Każda kategoria ma własny zestaw pól technicznych:

| Kategoria | Pola specyficzne |
|-----------|-----------------|
| Akumulatory | Typ, pojemność Ah, prąd rozruchowy A, napięcie, polaryzacja, wymiary |
| Prostowniki | Typ, napięcie, prąd ładowania A, zakres Ah |
| Oleje i smary | Lepkość SAE, specyfikacja API/ACEA, pojemność, normy OEM |
| Chemia samochodowa | Rodzaj, pojemność, forma (spray / płyn / pianka) |
| Płyny eksploatacyjne | Rodzaj, pojemność, sezon, norma DOT, kolor, temperatura |
| Żarówki | Typ (H1–H16, LED, ksenon), napięcie, moc W, trzonek |
| Akcesoria | Rodzaj, rozmiar, materiał |

---

## Stos technologiczny

| Pakiet | Zastosowanie |
|--------|-------------|
| Microsoft.Data.Sqlite 8.0.0 | Baza danych SQLite |
| System.IO.Ports 8.0.0 | Komunikacja COM z kasą fiskalną |
| CommunityToolkit.Mvvm 8.2.2 | Wzorzec MVVM |
| QuestPDF 2024.12.0 | Generowanie faktur PDF |
| MailKit 4.8.0 | Wysyłka e-mail |
| ClosedXML 0.104.2 | Eksport do Excela |

---

## Licencja

Copyright (c) 2025 ByakkoHex. Wszelkie prawa zastrzeżone.  
Szczegóły w pliku [LICENSE](LICENSE).
