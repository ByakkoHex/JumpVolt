# 🤖 Claude Code — Jak używać Claude do edycji projektu

## Co to jest Claude Code?

Claude Code to narzędzie konsolowe, które pozwala Claude **bezpośrednio edytować pliki**
w Twoim projekcie. Zamiast kopiować kod z czatu — mówisz Claude co zmienić,
a on sam otwiera pliki, edytuje je i zapisuje. Rider od razu widzi zmiany.

---

## ⚡ Szybki start (5 minut)

### 1. Zainstaluj Node.js (jeśli nie masz)

Pobierz z: https://nodejs.org/en/download
Wybierz **LTS** → Windows Installer (.msi) → zainstaluj z domyślnymi opcjami.

Sprawdź w terminalu:
```powershell
node --version
# powinno pokazać np. v20.x.x
```

### 2. Zainstaluj Claude Code

Otwórz **PowerShell** lub **Terminal** i wpisz:
```powershell
npm install -g @anthropic-ai/claude-code
```

### 3. Przejdź do folderu projektu

```powershell
cd "Z:\Aplikacja dla taty\SklepMotoryzacyjny"
```

### 4. Uruchom Claude Code

```powershell
claude
```

Przy pierwszym uruchomieniu poprosi o klucz API Anthropic.
Klucz możesz wygenerować na: https://console.anthropic.com/settings/keys

### 5. Gadaj z Claude!

Teraz jesteś w trybie interaktywnym. Mów po polsku co chcesz zmienić:

```
> Zmień kolor sidebara na ciemniejszy

> Dodaj pole "numer VIN" do formularza akumulatorów

> W ekranie sprzedaży dodaj przycisk "Rabat 10%"

> Popraw błąd w NovitusFiscalService - timeout jest za krótki

> Pokaż mi plik MainWindow.xaml
```

Claude sam:
- Otworzy odpowiednie pliki
- Znajdzie miejsce do zmiany
- Zedytuje kod
- Zapisze plik

Ty tylko akceptujesz zmiany (Enter) lub odrzucasz (n).

---

## 📋 Przydatne komendy w Claude Code

| Komenda | Co robi |
|---------|---------|
| `claude` | Uruchamia tryb interaktywny |
| `claude "zrób X"` | Jednorazowe polecenie bez trybu interaktywnego |
| `claude --help` | Pomoc |
| Ctrl+C | Przerwij bieżące zadanie |
| `/exit` | Wyjdź z Claude Code |

## 💡 Przykłady poleceń dla tego projektu

```
> Dodaj nową kategorię "Filtry" z polami: typ filtra, średnica, gwint

> W SalesView dodaj duży przycisk "Szybka sprzedaż akumulatora"

> Zmień czcionkę w całej aplikacji na większą

> Dodaj eksport historii sprzedaży do CSV

> Popraw formularz - przy wyborze kategorii "Żarówki" pokaż tylko odpowiednie typy

> Zrób backup bazy danych przy starcie aplikacji
```

---

## ⚠️ Ważne uwagi

1. **Rider widzi zmiany natychmiast** — po edycji przez Claude Code
   pliki są od razu zaktualizowane. Rider pokaże zmiany w edytorze.

2. **Git** — warto mieć Git w projekcie, żeby móc cofnąć zmiany:
   ```powershell
   cd "Z:\Aplikacja dla taty\SklepMotoryzacyjny"
   git init
   git add .
   git commit -m "Wersja początkowa"
   ```
   Wtedy jeśli Claude coś zepsuje, możesz cofnąć:
   ```powershell
   git checkout -- .
   ```

3. **Koszt** — Claude Code używa API Anthropic (płatne za tokeny).
   Typowa sesja edycji to ok. $0.50-2.00.

4. **Alternatywa darmowa** — możesz też dalej wklejać mi błędy
   i zmiany tutaj na czacie, a ja dam Ci gotowy kod do podmianki.
