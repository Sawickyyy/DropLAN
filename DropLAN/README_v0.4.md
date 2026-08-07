# DropLAN 0.4

## Nowe

### iPhone / web app
- manifest PWA
- ikona Home Screen
- tryb standalone
- instrukcja „Dodaj do ekranu początkowego”
- service worker przygotowany pod HTTPS
- UI nadal działa bez CDN

### Windows
- tray / zasobnik systemowy
- zamknięcie okna nie wyłącza serwera
- menu tray:
  - Otwórz
  - Kopiuj adres
  - Nowa sesja
  - Otwórz folder
  - Zakończ
- powiadomienia po odebraniu pliku

### Aktualizacje
- Velopack bootstrap
- UpdateManager + GitHub Releases
- przycisk „Sprawdź aktualizacje”
- download progress
- instalacja i restart aplikacji
- skrypt budujący Setup.exe
- skrypt publikacji na GitHub
- GitHub Actions workflow

## WAŻNE: ustaw repozytorium

W `UpdateSettings.cs` zmień:

```csharp
"https://github.com/CHANGE_ME/DropLAN"
```

na prawdziwy URL repozytorium.

## Installer

PowerShell:

```powershell
.\Scripts\build-release.ps1 -Version 0.4.0
```

W katalogu `Releases` pojawi się instalator Velopack i paczki aktualizacji.

## GitHub release

Możesz użyć workflow `.github/workflows/release.yml`.

GitHub -> Actions -> Build DropLAN Release -> Run workflow -> podaj np. `0.4.1`.

## PWA / iPhone

Na iPhonie:
1. zeskanuj QR,
2. sparuj PIN-em,
3. Safari -> Udostępnij,
4. Dodaj do ekranu początkowego.

### Ograniczenie HTTP

DropLAN działa obecnie na lokalnym HTTP.
iPhone może uruchamiać stronę z ekranu początkowego w trybie aplikacji,
ale service worker wymaga bezpiecznego kontekstu (HTTPS).

Pełne offline/cache PWA włączymy po dodaniu lokalnego HTTPS.
