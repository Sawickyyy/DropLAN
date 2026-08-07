# DropLAN 0.5 - refaktor backendu i frontendu

## Co się zmieniło

`LocalServer.cs` nie zawiera już całej aplikacji webowej ani wszystkich endpointów.

Nowa struktura:

```text
DropLAN/
├── LocalServer.cs
├── Routes/
│   ├── PwaRoutes.cs
│   ├── PairRoutes.cs
│   ├── StateRoutes.cs
│   ├── EventRoutes.cs
│   ├── UploadRoutes.cs
│   ├── DownloadRoutes.cs
│   └── ClipboardRoutes.cs
├── Services/
│   ├── PairingSession.cs
│   └── FileTransferHelpers.cs
└── WebAssets/
    ├── index.html
    ├── style.css
    ├── app.js
    ├── manifest.webmanifest
    ├── sw.js
    └── icons...
```

## Najważniejsza poprawka

W `StartAsync()` używany jest teraz lokalny, nienullowalny `app`:

```csharp
var app = builder.Build();
_app = app;
```

Endpointy są mapowane na `app`, więc znika ostrzeżenie `CS8604` dotyczące możliwego null `_app`.

## Frontend

HTML, CSS i JS zostały wyciągnięte 1:1 z dotychczasowego `WebPage` do osobnych plików.
Dzięki temu Visual Studio normalnie podświetla składnię i nie trzeba edytować 900-liniowego raw stringa w C#.

## Test po podmianie

```powershell
dotnet clean
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true
```

Następnie uruchom DropLAN i sprawdź:

1. QR + PIN,
2. iPhone -> PC,
3. PC -> iPhone,
4. aktualizacje SSE bez refresh,
5. clipboard,
6. PWA / ikonę Home Screen,
7. tray.
