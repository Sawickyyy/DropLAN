# DropLAN v0.3

Wersja demonstracyjna / MVP:
- iPhone -> Windows
- Windows -> iPhone
- wiele plików
- drag & drop na Windows
- QR
- PIN + token sesji
- historia transferów
- folder odbiorczy
- schowek tekstowy
- aktualizacje strony w czasie rzeczywistym przez Server-Sent Events (SSE)
- progress upload/download
- nowoczesny interfejs WPF i web
- brak zewnętrznego CDN, więc UI telefonu działa w LAN bez internetu

## Uruchomienie

1. Otwórz `DropLAN.csproj` w Visual Studio 2026.
2. Przywróć pakiety NuGet.
3. Uruchom projekt.
4. Zeskanuj QR z iPhone'a.
5. Wpisz PIN pokazany w aplikacji Windows.

## Ważne

Transfer jest lokalny, ale aktualna wersja używa HTTP, więc nie zapewnia szyfrowania transportu.
PIN/token chronią przed przypadkowym dostępem innych urządzeń w LAN, ale nie zastępują HTTPS.
