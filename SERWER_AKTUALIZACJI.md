# Poradnik: Serwer dla JumpVolt

Plik opisuje dwa niezależne tematy:
1. **Serwer aktualizacji** — hostowanie pliku `version.json` + instalatora `.exe`
2. **Serwer bazy danych** — współdzielona baza SQLite na NAS/serwerze dla wielu stanowisk

---

# Część 1: Serwer aktualizacji

Masz 3 opcje — od najprostszej do najbardziej profesjonalnej:

---

## Opcja A: GitHub Releases (bezpłatne, zero serwera)

Najłatwiejsza opcja jeśli kod jest lub będzie na GitHubie.

**1. Utwórz repozytorium** na github.com i idź do **Releases → Draft a new release**

**2. Utwórz plik `version.json`** i wrzuć go do repo (np. w katalogu `releases/`):
```json
{
  "version": "1.1.0",
  "downloadUrl": "https://github.com/TWÓJ_LOGIN/jumpvolt/releases/download/v1.1.0/JumpVolt-Setup-1.1.0.exe",
  "changelog": "- Poprawka X\n- Nowa funkcja Y"
}
```

**3. W ustawieniach aplikacji wpisz URL do raw pliku:**
```
https://raw.githubusercontent.com/TWÓJ_LOGIN/jumpvolt/main/releases/version.json
```

**Aktualizacja wersji:** edytujesz `version.json` w repo + dodajesz nowy Release z `.exe` jako asset. Gotowe.

---

## Opcja B: VM lokalna (testy)

Zakładam Windows z Hyper-V lub VirtualBox. Używamy **Ubuntu Server 24.04 + nginx**.

### 1. Utwórz VM

- RAM: 512 MB, dysk: 10 GB, sieć: **mostkowana** (bridge) — VM dostanie IP w Twojej sieci
- Zainstaluj Ubuntu Server (opcja minimalna, bez GUI)

### 2. Zainstaluj nginx

```bash
sudo apt update && sudo apt install -y nginx
sudo systemctl enable nginx
```

### 3. Utwórz katalog na pliki aktualizacji

```bash
sudo mkdir -p /var/www/jumpvolt
sudo chown $USER:$USER /var/www/jumpvolt
```

### 4. Skonfiguruj nginx

```bash
sudo nano /etc/nginx/sites-available/jumpvolt
```

Wklej:
```nginx
server {
    listen 80;
    server_name _;          # na VM akceptuje każdy hostname

    root /var/www/jumpvolt;
    autoindex on;           # można przeglądać pliki w przeglądarce (wygodne do testów)

    location / {
        try_files $uri $uri/ =404;
        add_header Cache-Control "no-cache";    # ważne — JSON nie może być cache'owany
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/jumpvolt /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl restart nginx
```

### 5. Wrzuć pliki

```bash
# Na maszynie deweloperskiej (Windows) — scp lub WinSCP
scp JumpVolt-Setup-1.1.0.exe uzytkownik@IP_VM:/var/www/jumpvolt/
```

Utwórz `version.json`:
```bash
nano /var/www/jumpvolt/version.json
```
```json
{
  "version": "1.1.0",
  "downloadUrl": "http://IP_VM/JumpVolt-Setup-1.1.0.exe",
  "changelog": "- Pierwsza wersja testowa"
}
```

### 6. Sprawdź IP VM i wpisz w aplikacji

```bash
ip addr show | grep inet
```

W ustawieniach JumpVolt (Ustawienia → Aktualizacje):
```
http://192.168.1.XX/version.json
```

---

## Opcja C: Serwer produkcyjny (VPS + HTTPS)

### 1. Kup VPS

Polecane dla małej aplikacji (tani, niezawodny):
- **Hetzner** CX22 — ~4€/mies., Niemcy/Finlandia
- **Contabo** VPS S — ~5€/mies., Niemcy
- **DigitalOcean** Droplet — ~6$/mies.

Wybierz **Ubuntu 24.04**, zapamiętaj IP serwera.

### 2. Pierwsze kroki po SSH

```bash
ssh root@IP_SERWERA

# Utwórz użytkownika (nie pracuj jako root)
adduser jumpvolt
usermod -aG sudo jumpvolt
su - jumpvolt

# Aktualizacja systemu
sudo apt update && sudo apt upgrade -y
```

### 3. Zainstaluj nginx + certbot (HTTPS)

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
sudo systemctl enable nginx
```

### 4. Kup domenę lub użyj darmowej

- Domena `.pl` ~50 zł/rok (np. przez home.pl, nazwa.pl)
- Darmowa subdomena przez **duckdns.org** (np. `jumpvolt.duckdns.org`)

Ustaw rekord DNS **A** domeny → IP serwera. Poczekaj 5–30 min na propagację.

### 5. Skonfiguruj nginx

```bash
sudo nano /etc/nginx/sites-available/jumpvolt
```

```nginx
server {
    listen 80;
    server_name twoja-domena.pl;    # lub jumpvolt.duckdns.org

    root /var/www/jumpvolt;

    location / {
        try_files $uri $uri/ =404;
        add_header Cache-Control "no-cache, no-store, must-revalidate";
        add_header Access-Control-Allow-Origin "*";
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/jumpvolt /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo mkdir -p /var/www/jumpvolt
sudo chown jumpvolt:jumpvolt /var/www/jumpvolt
sudo nginx -t && sudo systemctl restart nginx
```

### 6. Włącz HTTPS (Let's Encrypt — bezpłatne)

```bash
sudo certbot --nginx -d twoja-domena.pl
# Podaj email, zaakceptuj warunki
# Certbot sam edytuje nginx i ustawia auto-odnowienie
```

Sprawdź czy auto-odnowienie działa:
```bash
sudo certbot renew --dry-run
```

### 7. Wrzuć pliki aktualizacji

Na serwerze utwórz skrypt do deployu — będziesz go używał przy każdej nowej wersji:

```bash
nano ~/deploy-update.sh
```

```bash
#!/bin/bash
# Użycie: ./deploy-update.sh 1.2.0 /ścieżka/do/JumpVolt-Setup-1.2.0.exe
VERSION=$1
EXE_PATH=$2
DEPLOY_DIR=/var/www/jumpvolt

cp "$EXE_PATH" "$DEPLOY_DIR/JumpVolt-Setup-$VERSION.exe"

cat > "$DEPLOY_DIR/version.json" << EOF
{
  "version": "$VERSION",
  "downloadUrl": "https://twoja-domena.pl/JumpVolt-Setup-$VERSION.exe",
  "changelog": "Aktualizacja do wersji $VERSION"
}
EOF

echo "Wdrożono wersję $VERSION"
ls -lh "$DEPLOY_DIR"
```

```bash
chmod +x ~/deploy-update.sh
```

**Przy wydaniu nowej wersji (z Windows):**
```bash
# Skopiuj exe na serwer
scp JumpVolt-Setup-1.2.0.exe jumpvolt@twoja-domena.pl:~/

# Na serwerze uruchom deploy
ssh jumpvolt@twoja-domena.pl
./deploy-update.sh 1.2.0 ~/JumpVolt-Setup-1.2.0.exe
```

W ustawieniach aplikacji (Ustawienia → Aktualizacje):
```
https://twoja-domena.pl/version.json
```

---

## Porównanie opcji (serwer aktualizacji)

| | GitHub | VM lokalna | VPS produkcyjny |
|---|---|---|---|
| Koszt | Bezpłatne | Bezpłatne | ~4–6€/mies. |
| HTTPS | Tak (automatycznie) | Nie (tylko http) | Tak (Let's Encrypt) |
| Dostępność z zewnątrz | Tak | Nie (tylko LAN) | Tak |
| Trudność setup | Bardzo łatwe | Łatwe | Średnie |
| Polecane do | Produkcja (jeśli masz GitHub) | Testów | Produkcja (własny serwer) |

**Rekomendacja:** Zacznij od **GitHub Releases** — zero kosztów, zero serwera, działa od razu.
Jeśli chcesz pełną kontrolę bez zależności od GitHub → VPS z nginx.

---

---

# Część 2: Serwer bazy danych (wiele stanowisk)

Aplikacja działa domyślnie z lokalnym SQLite. Jeśli chcesz, żeby **kilka komputerów w sklepie dzieliło tę samą bazę** (np. kasa + komputer biurowy), ustaw bazę na serwerze/NAS.

**Architektura:** SQLite na udostępnionym folderze sieciowym (Samba/SMB).
- Brak nowych technologii — ta sama baza, ta sama aplikacja
- Działa dla 1–3 równoczesnych użytkowników (mały sklep)
- Dla większych instalacji → PostgreSQL (patrz na końcu)

---

## Opcja D: NAS domowy / istniejący serwer Windows

Jeśli masz w sieci NAS (Synology, QNAP) lub komputer Windows z udostępnionym folderem:

### 1. Utwórz udostępniony folder

**Na Windows:**
- Utwórz folder `C:\JumpVoltDB\`
- Kliknij prawym → Właściwości → Udostępnianie → Udostępnij
- Ustaw uprawnienia: `Wszyscy` → Odczyt/Zapis (lub konkretne konta)
- Zapamiętaj ścieżkę UNC: `\\NAZWA_KOMPUTERA\JumpVoltDB`

**Na NAS Synology/QNAP:**
- Panel sterowania → Folder udostępniony → Utwórz `JumpVolt`
- Protokół: SMB/CIFS włączony
- Zapamiętaj ścieżkę: `\\IP_NAS\JumpVolt`

### 2. Skonfiguruj aplikację

W **Ustawienia → Baza danych**:
- Tryb: **Sieciowy**
- Ścieżka: `\\NAZWA_KOMPUTERA\JumpVoltDB\jumpvolt.db`

Kliknij **"Skopiuj lokalną bazę na serwer"** (przeniesie istniejące dane).

Kliknij **Zapisz ustawienia** → zrestartuj aplikację.

Powtórz konfigurację na każdym komputerze w sklepie (tylko zmiana ścieżki, bez kopiowania ponownie).

---

## Opcja E: Ubuntu Server jako serwer plików (Samba)

Idealne gdy chcesz dedykowany serwer tylko do bazy (np. stary komputer lub VM).

### 1. Zainstaluj Sambę

```bash
sudo apt update && sudo apt install -y samba
```

### 2. Utwórz folder i użytkownika Samby

```bash
# Utwórz folder
sudo mkdir -p /srv/jumpvolt
sudo chmod 777 /srv/jumpvolt

# Utwórz użytkownika Samby (może być istniejący użytkownik Linux)
sudo smbpasswd -a $USER
# Wpisz hasło (może być inne niż systemowe)
```

### 3. Skonfiguruj udział Samby

```bash
sudo nano /etc/samba/smb.conf
```

Na końcu pliku dodaj:
```ini
[JumpVolt]
   path = /srv/jumpvolt
   browseable = yes
   read only = no
   create mask = 0666
   directory mask = 0777
   valid users = TWÓJ_USER
   comment = JumpVolt baza danych
```

```bash
sudo systemctl restart smbd
sudo systemctl enable smbd
```

### 4. Sprawdź dostępność z Windows

W Eksploratorze Windows wpisz w pasku adresu:
```
\\IP_SERWERA\JumpVolt
```
Powinien zapytać o hasło Samby — wpisz to z `smbpasswd`.

### 5. Zamapuj dysk sieciowy (opcjonalnie, dla wygody)

W Windows: Eksplorator → Ten komputer → Mapuj dysk sieciowy
- Dysk: `Z:`
- Folder: `\\IP_SERWERA\JumpVolt`
- Zaznacz: Połącz ponownie podczas logowania + Zaloguj z innymi poświadczeniami

Wtedy ścieżka w aplikacji będzie prostsza:
```
Z:\jumpvolt.db
```

### 6. Konfiguracja aplikacji

W **Ustawienia → Baza danych** na każdym stanowisku:
- Tryb: **Sieciowy**
- Ścieżka: `\\192.168.1.XX\JumpVolt\jumpvolt.db` (lub `Z:\jumpvolt.db`)

Na pierwszym stanowisku kliknij **"Skopiuj lokalną bazę na serwer"**.

---

## Opcja F: Backup automatyczny bazy

Niezależnie od trybu (lokalny czy sieciowy) warto robić kopie zapasowe.

```bash
# Na Ubuntu — cron co godzinę kopiuje bazę
crontab -e
```

Dodaj linię:
```
0 * * * * cp /srv/jumpvolt/jumpvolt.db /srv/jumpvolt/backup/jumpvolt_$(date +\%Y\%m\%d_\%H).db
```

Albo prosty skrypt na Windows (Harmonogram zadań):
```batch
@echo off
set SRC=\\SERWER\JumpVolt\jumpvolt.db
set DST=C:\Backup\JumpVolt\jumpvolt_%date:~6,4%%date:~3,2%%date:~0,2%.db
xcopy "%SRC%" "%DST%" /Y
```

---

## Ważne uwagi techniczne

| Kwestia | Szczegóły |
|---|---|
| Jednoczesny dostęp | SQLite na sieci działa bezpiecznie dla 1–3 użytkowników pisących rzadko jednocześnie |
| Opóźnienie sieci | Lokalna sieć LAN (1 Gbit) jest wystarczająca. WiFi też działa, ale wolniej |
| Offline | Jeśli serwer niedostępny → aplikacja nie uruchomi się (nie znajdzie bazy). Zachowaj kopię lokalną! |
| Kopia bezpieczeństwa | Zawsze miej aktualną kopię lokalną — patrz Opcja F |
| Windows vs Linux ścieżki | Ścieżka UNC `\\SERWER\folder\plik.db` działa na Windows bez dodatkowej konfiguracji |

---

## Kiedy PostgreSQL?

SQLite na sieci wystarczy dla małego sklepu. PostgreSQL jest potrzebny gdy:
- Więcej niż 5 komputerów pisze jednocześnie
- Potrzebujesz dostępu przez internet (nie tylko LAN)
- Chcesz centralnego serwera zarządzanego przez admina

Wdrożenie PostgreSQL wymaga zmiany w kodzie aplikacji (nowy pakiet `Npgsql`, adaptacja SQL) — można to zrobić w przyszłości jako osobny etap.

---

## Podsumowanie — co wybrać?

| Scenariusz | Zalecenie |
|---|---|
| 1 komputer w sklepie | Lokalny SQLite (domyślnie) |
| 2–3 komputery, jeden sklep, ta sama sieć LAN | Samba na istniejącym NAS lub Ubuntu VM (Opcja D lub E) |
| Dostęp z zewnątrz (np. właściciel z domu) | VPN do sieci sklepu + Samba LAN |
| Wiele sklepów / duże obciążenie | PostgreSQL (wymaga zmian w kodzie) |
