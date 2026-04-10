# Plan rozwoju JumpVolt

Priorytety oznaczone jako: 🔴 wysoki · 🟡 średni · 🟢 niski

---

## Sprzedaż i kasa

- 🔴 Skróty klawiszowe przy kasie — F1-F4 dla metod płatności, Enter zatwierdza, Esc anuluje
- 🔴 Szybkie rabaty — pole "%" przy pozycji w koszyku lub na całym paragonie
- 🟡 Redruk ostatniego paragonu (jeden przycisk)
- 🟡 Wstrzymanie paragonu — możliwość "zawieszenia" koszyka i otwarcia nowego, potem powrót
- 🟡 Stan kasy — raport ile gotówki powinno być w szufladzie (suma sprzedaży gotówkowej od raportu Z)
- 🟢 Historia ostatnich 5 sprzedaży widoczna z poziomu ekranu sprzedaży

---

## Magazyn i produkty

- 🔴 Alert niskiego stanu — ostrzeżenie gdy produkt schodzi poniżej ustalonego progu (np. < 3 szt.)
- 🔴 Automatyczne przeliczanie netto ↔ brutto przy wpisywaniu ceny (z domyślną stawką VAT)
- 🟡 Kody QR produktów — generowanie i drukowanie etykiety z kodem QR i ceną
- 🟡 Drukowanie etykiet cenowych (PDF gotowy do wydruku na drukarce etykiet)
- 🟡 Import produktów z pliku Excel / CSV
- 🟢 Historia zmian ceny produktu
- 🟢 Produkty powiązane / zamienniki (np. akumulator pasuje do tych modeli aut)

---

## Faktury i dokumenty

- 🔴 Faktura VAT do paragonu — wystawienie faktury do już istniejącego paragonu
- 🔴 Automatyczne netto/brutto w fakturze z wyborem stawki VAT per pozycja
- 🟡 Numeracja faktur — własna seria (np. FV/2025/001)
- 🟡 Wysyłka faktury e-mailem z poziomu historii sprzedaży
- 🟡 Korekta faktury
- 🟢 Eksport faktur do folderu miesięcznego (auto-archiwum PDF)

---

## Raporty i statystyki

- 🔴 Dashboard — przychód dziś / ten tydzień / ten miesiąc na ekranie głównym
- 🟡 Najlepiej sprzedające się produkty (Top 10)
- 🟡 Raport sprzedaży per kategoria
- 🟡 Wykres sprzedaży dzienny / miesięczny
- 🟢 Eksport raportu miesięcznego do Excela jednym kliknięciem

---

## Klienci

- 🟡 Baza klientów — imię/firma, NIP, adres, e-mail, telefon
- 🟡 Powiązanie sprzedaży z klientem (historia zakupów klienta)
- 🟡 Szybkie wyszukanie klienta po NIP przy wystawianiu faktury
- 🟢 Notatki do klienta (np. "ma akumulator na gwarancji do 2026-03")

---

## UI i komfort pracy

- 🔴 Ciemny / jasny motyw (przełącznik w ustawieniach)
- 🟡 Poprawa czytelności tabel — większa czcionka, lepsze odstępy
- 🟡 Potwierdzenia akcji — "Czy na pewno usunąć produkt?" żeby nie było przypadkowych kliknięć
- 🟡 Powiadomienia o niskim stanie magazynu przy starcie aplikacji
- 🟢 Możliwość zmiany kolejności kolumn w tabelach

---

## Techniczne i infrastruktura

- 🟡 Automatyczna kopia zapasowa bazy danych (już jest BackupService — podpiąć do harmonogramu)
- 🟡 Eksport / import całej bazy (przeniesienie danych na nowy komputer)
- 🟢 Logi błędów zapisywane do pliku (żeby łatwiej diagnozować problemy)
- 🟢 Tryb offline z synchronizacją gdy wróci sieć (dla trybu sieciowej bazy)

---

## Zrobione

- ✅ Sprzedaż z kasą fiskalną Novitus (COM / TCP/IP)
- ✅ Magazyn z kategoriami i dynamicznymi polami technicznymi
- ✅ Historia sprzedaży z filtrami i eksportem Excel
- ✅ Zwroty z przywróceniem stanu magazynu
- ✅ Faktury PDF z wysyłką e-mail
- ✅ Raporty fiskalne X i Z
- ✅ Kopie zapasowe bazy danych
- ✅ Tryb sieciowej bazy danych
- ✅ Autoaktualizacja przez GitHub Releases
- ✅ Uruchamianie z Windows (rejestr)
