using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace DropLAN;

public sealed class LocalServer
{
    private const string SessionCookieName = "DropLANSession";
    private const int Port = 5050;

    private readonly SharedState _state;
    private readonly RealtimeBroker _realtime;
    private readonly object _sessionSync = new();

    private WebApplication? _app;
    private string _pairToken = "";
    private string _pairPin = "";

    public LocalServer(SharedState state, RealtimeBroker realtime)
    {
        _state = state;
        _realtime = realtime;

        RegenerateSession();

        _state.Changed += () => _realtime.Publish();
    }

    public string PairPin
    {
        get
        {
            lock (_sessionSync)
                return _pairPin;
        }
    }

    public string GetPairUrl()
    {
        var ip = NetworkHelper.GetLocalIPv4();

        lock (_sessionSync)
            return $"http://{ip}:{Port}/?token={Uri.EscapeDataString(_pairToken)}";
    }

    public void RegenerateSession()
    {
        lock (_sessionSync)
        {
            _pairToken = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(24))
                .ToLowerInvariant();

            _pairPin = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();
        }

        _realtime.Publish("session");
    }

    public async Task StartAsync()
    {
        if (_app != null)
            return;

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize =
                16L * 1024 * 1024 * 1024;
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit =
                16L * 1024 * 1024 * 1024;
        });

        _app = builder.Build();

        MapPwaAssets();
        MapHome();
        MapPairing();
        MapState();
        MapEvents();
        MapUpload();
        MapDownload();
        MapClipboard();

        await _app.StartAsync();
    }


    private void MapPwaAssets()
    {
        var assets = Path.Combine(
            AppContext.BaseDirectory,
            "WebAssets");

        _app!.MapGet(
            "/manifest.webmanifest",
            () => Results.File(
                Path.Combine(assets, "manifest.webmanifest"),
                "application/manifest+json"));

        _app.MapGet(
            "/sw.js",
            () => Results.File(
                Path.Combine(assets, "sw.js"),
                "application/javascript"));

        _app.MapGet(
            "/icon-192.png",
            () => Results.File(
                Path.Combine(assets, "icon-192.png"),
                "image/png"));

        _app.MapGet(
            "/icon-512.png",
            () => Results.File(
                Path.Combine(assets, "icon-512.png"),
                "image/png"));

        _app.MapGet(
            "/apple-touch-icon.png",
            () => Results.File(
                Path.Combine(assets, "apple-touch-icon.png"),
                "image/png"));
    }

    private void MapHome()
    {
        _app!.MapGet("/", async context =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(WebPage);
        });
    }

    private void MapPairing()
    {
        _app!.MapPost("/api/pair", async context =>
        {
            var request = await context.Request.ReadFromJsonAsync<PairRequest>(
                cancellationToken: context.RequestAborted);

            if (request == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            string expectedToken;
            string expectedPin;

            lock (_sessionSync)
            {
                expectedToken = _pairToken;
                expectedPin = _pairPin;
            }

            var tokenOk = CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(request.Token ?? ""),
                System.Text.Encoding.UTF8.GetBytes(expectedToken));

            var pinOk = string.Equals(
                request.Pin,
                expectedPin,
                StringComparison.Ordinal);

            if (!tokenOk || !pinOk)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(
                    new { message = "Nieprawidłowy kod PIN." },
                    context.RequestAborted);

                return;
            }

            context.Response.Cookies.Append(
                SessionCookieName,
                expectedToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromHours(8)
                });

            await context.Response.WriteAsJsonAsync(
                new { ok = true },
                context.RequestAborted);
        });

        _app!.MapPost("/api/logout", async context =>
        {
            context.Response.Cookies.Delete(SessionCookieName);
            await context.Response.WriteAsJsonAsync(
                new { ok = true },
                context.RequestAborted);
        });
    }

    private void MapState()
    {
        _app!.MapGet("/api/state", async context =>
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var files = _state.GetSharedFiles()
                .Select(file => new
                {
                    id = file.Id,
                    name = file.Name,
                    size = file.Size,
                    addedAt = file.AddedAt
                });

            var history = _state.GetHistory()
                .Take(30)
                .Select(item => new
                {
                    time = item.Time,
                    fileName = item.FileName,
                    size = item.Size,
                    direction = item.Direction.ToString(),
                    status = item.Status
                });

            await context.Response.WriteAsJsonAsync(
                new
                {
                    online = true,
                    files,
                    clipboard = _state.ClipboardText,
                    history,
                    connectedClients = _realtime.ClientCount
                },
                context.RequestAborted);
        });
    }

    private void MapEvents()
    {
        _app!.MapGet("/events", async context =>
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.ContentType = "text/event-stream";

            var reader = _realtime.Subscribe(context.RequestAborted);

            await context.Response.WriteAsync(
                "data: connected\n\n",
                context.RequestAborted);

            await context.Response.Body.FlushAsync(context.RequestAborted);

            try
            {
                await foreach (var message in reader.ReadAllAsync(context.RequestAborted))
                {
                    await context.Response.WriteAsync(
                        $"data: {message}\n\n",
                        context.RequestAborted);

                    await context.Response.Body.FlushAsync(
                        context.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Klient zamknął stronę.
            }
        });
    }

    private void MapUpload()
    {
        _app!.MapPost("/upload", async context =>
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (!context.Request.HasFormContentType)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var form = await context.Request.ReadFormAsync(
                context.RequestAborted);

            if (form.Files.Count == 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var targetFolder = _state.DownloadFolder;
            Directory.CreateDirectory(targetFolder);

            var saved = new List<object>();

            foreach (var file in form.Files)
            {
                if (file.Length <= 0)
                    continue;

                var safeName = SanitizeFileName(file.FileName);

                if (string.IsNullOrWhiteSpace(safeName))
                    continue;

                var destination = GetUniqueFilePath(
                    targetFolder,
                    safeName);

                try
                {
                    await using var output = new FileStream(
                        destination,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        1024 * 128,
                        useAsync: true);

                    await file.CopyToAsync(
                        output,
                        context.RequestAborted);

                    _state.AddHistory(
                        Path.GetFileName(destination),
                        file.Length,
                        TransferDirection.PhoneToPc,
                        "Zakończono");

                    saved.Add(new
                    {
                        name = Path.GetFileName(destination),
                        size = file.Length
                    });
                }
                catch (OperationCanceledException)
                {
                    _state.AddHistory(
                        safeName,
                        file.Length,
                        TransferDirection.PhoneToPc,
                        "Anulowano");

                    throw;
                }
            }

            await context.Response.WriteAsJsonAsync(
                new
                {
                    count = saved.Count,
                    files = saved
                },
                context.RequestAborted);
        });
    }

    private void MapDownload()
    {
        _app!.MapGet("/download/{id:guid}", async context =>
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var idText = context.Request.RouteValues["id"]?.ToString();

            if (!Guid.TryParse(idText, out var id))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var file = _state.FindSharedFile(id);

            if (file == null || !File.Exists(file.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength = file.Size;
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.ContentDisposition =
                $"attachment; filename*=UTF-8''{Uri.EscapeDataString(file.Name)}";

            try
            {
                await context.Response.SendFileAsync(
                    file.Path,
                    context.RequestAborted);

                _state.AddHistory(
                    file.Name,
                    file.Size,
                    TransferDirection.PcToPhone,
                    "Zakończono");
            }
            catch (OperationCanceledException)
            {
                _state.AddHistory(
                    file.Name,
                    file.Size,
                    TransferDirection.PcToPhone,
                    "Anulowano");
            }
        });
    }

    private void MapClipboard()
    {
        _app!.MapPost("/api/clipboard", async context =>
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var request = await context.Request.ReadFromJsonAsync<ClipboardRequest>(
                cancellationToken: context.RequestAborted);

            if (request == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            _state.SetClipboard(request.Text ?? "");

            await context.Response.WriteAsJsonAsync(
                new { ok = true },
                context.RequestAborted);
        });
    }

    private bool IsAuthorized(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(
                SessionCookieName,
                out var cookie))
            return false;

        string token;

        lock (_sessionSync)
            token = _pairToken;

        if (cookie.Length != token.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(cookie),
            System.Text.Encoding.UTF8.GetBytes(token));
    }

    private static string SanitizeFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);

        foreach (var invalid in Path.GetInvalidFileNameChars())
            safe = safe.Replace(invalid, '_');

        return safe.Trim();
    }

    private static string GetUniqueFilePath(
        string directory,
        string fileName)
    {
        var first = Path.Combine(directory, fileName);

        if (!File.Exists(first))
            return first;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var counter = 1; ; counter++)
        {
            var candidate = Path.Combine(
                directory,
                $"{name} ({counter}){extension}");

            if (!File.Exists(candidate))
                return candidate;
        }
    }

    public async Task StopAsync()
    {
        if (_app == null)
            return;

        try
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        finally
        {
            _app = null;
        }
    }

    private const string WebPage = """
<!DOCTYPE html>
<html lang="pl">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <meta name="theme-color" content="#090b10">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
    <meta name="apple-mobile-web-app-title" content="DropLAN">

    <link rel="manifest" href="/manifest.webmanifest">
    <link rel="apple-touch-icon" href="/apple-touch-icon.png">

    <title>DropLAN</title>

    <style>
        :root {
            --bg: #080a0f;
            --surface: rgba(17, 20, 29, .82);
            --surface-2: rgba(28, 33, 46, .75);
            --border: rgba(255,255,255,.08);
            --text: #f7f8fb;
            --muted: #8f98aa;
            --accent: #7c5cff;
            --accent-2: #35c4ff;
            --good: #43d17a;
            --bad: #ff687d;
        }

        * { box-sizing: border-box; }

        html {
            color-scheme: dark;
            background: var(--bg);
        }

        body {
            margin: 0;
            min-height: 100vh;
            color: var(--text);
            font-family: Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            background:
                radial-gradient(circle at 20% 0%, rgba(124,92,255,.22), transparent 36rem),
                radial-gradient(circle at 100% 30%, rgba(53,196,255,.12), transparent 32rem),
                var(--bg);
            padding:
                max(22px, env(safe-area-inset-top))
                16px
                max(24px, env(safe-area-inset-bottom));
        }

        button, input, textarea { font: inherit; }

        .shell {
            width: min(760px, 100%);
            margin: 0 auto;
        }

        .topbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
            margin-bottom: 18px;
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .mark {
            width: 42px;
            height: 42px;
            border-radius: 14px;
            background: linear-gradient(135deg, var(--accent), var(--accent-2));
            display: grid;
            place-items: center;
            font-weight: 900;
            box-shadow: 0 12px 32px rgba(124,92,255,.3);
        }

        .brand h1 {
            margin: 0;
            font-size: 23px;
            letter-spacing: -.7px;
        }

        .brand p {
            margin: 2px 0 0;
            color: var(--muted);
            font-size: 12px;
        }

        .status-pill {
            border: 1px solid var(--border);
            background: rgba(255,255,255,.04);
            border-radius: 999px;
            padding: 8px 11px;
            color: var(--muted);
            font-size: 12px;
            white-space: nowrap;
        }

        .status-pill.online {
            color: #b9f7ce;
            background: rgba(67,209,122,.08);
            border-color: rgba(67,209,122,.18);
        }

        .grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 14px;
        }

        .card {
            border: 1px solid var(--border);
            background: var(--surface);
            backdrop-filter: blur(22px);
            -webkit-backdrop-filter: blur(22px);
            border-radius: 24px;
            padding: 20px;
            box-shadow: 0 24px 70px rgba(0,0,0,.22);
        }

        .full { grid-column: 1 / -1; }

        .card h2 {
            margin: 0;
            font-size: 16px;
            letter-spacing: -.3px;
        }

        .sub {
            margin: 7px 0 16px;
            color: var(--muted);
            font-size: 13px;
            line-height: 1.45;
        }

        .actions {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 8px;
        }

        .action-btn,
        .primary,
        .ghost,
        .download-btn {
            border: 0;
            border-radius: 14px;
            cursor: pointer;
            transition: .15s ease;
        }

        .action-btn {
            padding: 13px 8px;
            color: var(--text);
            background: var(--surface-2);
            border: 1px solid var(--border);
            font-size: 12px;
            font-weight: 650;
        }

        .action-btn:active,
        .primary:active,
        .ghost:active,
        .download-btn:active {
            transform: scale(.98);
        }

        .primary {
            width: 100%;
            padding: 14px 16px;
            margin-top: 12px;
            color: white;
            font-weight: 800;
            background: linear-gradient(135deg, var(--accent), #5d7cff);
        }

        .ghost {
            padding: 10px 12px;
            color: var(--text);
            background: rgba(255,255,255,.05);
            border: 1px solid var(--border);
        }

        .hidden { display: none !important; }

        .file-summary {
            min-height: 20px;
            margin-top: 12px;
            color: var(--muted);
            font-size: 12px;
            word-break: break-word;
        }

        .progress-wrap {
            display: none;
            margin-top: 12px;
        }

        .progress-track {
            height: 7px;
            border-radius: 999px;
            overflow: hidden;
            background: rgba(255,255,255,.08);
        }

        .progress-bar {
            width: 0%;
            height: 100%;
            background: linear-gradient(90deg, var(--accent), var(--accent-2));
            transition: width .08s linear;
        }

        .progress-text {
            margin-top: 6px;
            color: var(--muted);
            font-size: 11px;
            text-align: right;
        }

        .list {
            display: grid;
            gap: 9px;
        }

        .file-row {
            display: grid;
            grid-template-columns: minmax(0,1fr) auto;
            gap: 12px;
            align-items: center;
            padding: 12px;
            border: 1px solid var(--border);
            border-radius: 15px;
            background: rgba(255,255,255,.035);
        }

        .file-name {
            font-size: 13px;
            font-weight: 700;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .file-meta {
            margin-top: 3px;
            color: var(--muted);
            font-size: 11px;
        }

        .download-btn {
            padding: 9px 11px;
            color: white;
            background: rgba(124,92,255,.18);
            border: 1px solid rgba(124,92,255,.28);
            font-size: 12px;
            font-weight: 700;
        }

        textarea {
            width: 100%;
            min-height: 105px;
            resize: vertical;
            border: 1px solid var(--border);
            border-radius: 15px;
            outline: none;
            padding: 13px;
            color: var(--text);
            background: rgba(255,255,255,.035);
        }

        textarea:focus {
            border-color: rgba(124,92,255,.55);
            box-shadow: 0 0 0 3px rgba(124,92,255,.1);
        }

        .clip-actions {
            display: flex;
            gap: 8px;
            margin-top: 9px;
        }

        .history-row {
            padding: 10px 0;
            border-bottom: 1px solid rgba(255,255,255,.06);
        }

        .history-row:last-child { border-bottom: 0; }

        .history-main {
            display: flex;
            justify-content: space-between;
            gap: 12px;
            font-size: 12px;
        }

        .history-name {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .history-meta {
            margin-top: 4px;
            color: var(--muted);
            font-size: 11px;
        }

        .pair-card {
            max-width: 430px;
            margin: 11vh auto 0;
            text-align: center;
        }

        .pin {
            width: 100%;
            border: 1px solid var(--border);
            border-radius: 16px;
            background: rgba(255,255,255,.04);
            color: white;
            padding: 15px;
            font-size: 23px;
            text-align: center;
            letter-spacing: 8px;
            outline: none;
        }

        .message {
            min-height: 18px;
            margin-top: 10px;
            font-size: 12px;
            color: var(--muted);
        }

        .message.error { color: var(--bad); }
        .message.good { color: var(--good); }

        @media (max-width: 680px) {
            .grid { grid-template-columns: 1fr; }
            .full { grid-column: auto; }
            .topbar { align-items: flex-start; }
            .status-pill { margin-top: 4px; }
        }
    </style>
</head>

<body>

<div id="pairView" class="shell hidden">
    <div class="card pair-card">
        <div class="mark" style="margin:0 auto 14px;">D</div>
        <h2 style="font-size:22px;">Połącz z DropLAN</h2>
        <p class="sub">Wpisz 6-cyfrowy PIN wyświetlany na komputerze.</p>

        <input
            id="pinInput"
            class="pin"
            maxlength="6"
            inputmode="numeric"
            autocomplete="one-time-code"
            placeholder="000000">

        <button class="primary" onclick="pair()">Połącz urządzenie</button>
        <div id="pairMessage" class="message"></div>
    </div>
</div>

<div id="appView" class="shell hidden">

    <div class="topbar">
        <div class="brand">
            <div class="mark">D</div>
            <div>
                <h1>DropLAN</h1>
                <p>Transfer bez chmury, prosto po LAN</p>
            </div>
        </div>

        <div id="connectionPill" class="status-pill">
            Łączenie…
        </div>
    </div>

    <div id="installHint" class="card full" style="margin-bottom:14px; display:none;">
        <h2>Dodaj DropLAN do ekranu początkowego</h2>
        <p class="sub" style="margin-bottom:0;">
            Na iPhonie: Udostępnij → Dodaj do ekranu początkowego.
            Potem DropLAN otwiera się jak osobna aplikacja.
        </p>
    </div>

    <div class="grid">

        <section class="card">
            <h2>Wyślij na komputer</h2>
            <p class="sub">Zdjęcia, filmy, dokumenty albo dowolne pliki.</p>

            <div class="actions">
                <button class="action-btn" onclick="cameraInput.click()">📷 Aparat</button>
                <button class="action-btn" onclick="galleryInput.click()">🖼️ Galeria</button>
                <button class="action-btn" onclick="filesInput.click()">📁 Pliki</button>
            </div>

            <input id="cameraInput" class="hidden" type="file" accept="image/*" capture="environment">
            <input id="galleryInput" class="hidden" type="file" accept="image/*,video/*" multiple>
            <input id="filesInput" class="hidden" type="file" multiple>

            <div id="selectedSummary" class="file-summary">Nic nie wybrano.</div>

            <button id="uploadButton" class="primary" onclick="uploadSelected()">
                Wyślij
            </button>

            <div id="uploadProgressWrap" class="progress-wrap">
                <div class="progress-track">
                    <div id="uploadProgress" class="progress-bar"></div>
                </div>
                <div id="uploadProgressText" class="progress-text">0%</div>
            </div>

            <div id="uploadMessage" class="message"></div>
        </section>

        <section class="card">
            <h2>Schowek</h2>
            <p class="sub">Przerzuć tekst między iPhonem i Windowsem.</p>

            <textarea id="clipboardText" placeholder="Wklej tekst…"></textarea>

            <div class="clip-actions">
                <button class="ghost" onclick="saveClipboard()">Udostępnij</button>
                <button class="ghost" onclick="copyClipboard()">Kopiuj</button>
            </div>

            <div id="clipboardMessage" class="message"></div>
        </section>

        <section class="card full">
            <h2>Pliki z komputera</h2>
            <p class="sub">Lista aktualizuje się automatycznie, bez odświeżania strony.</p>
            <div id="downloadList" class="list"></div>
        </section>

        <section class="card full">
            <h2>Ostatnie transfery</h2>
            <p class="sub">Historia bieżącej sesji.</p>
            <div id="historyList"></div>
        </section>

    </div>
</div>

<script>
    const params = new URLSearchParams(location.search);
    const pairToken = params.get("token") || "";

    const pairView = document.getElementById("pairView");
    const appView = document.getElementById("appView");

    const cameraInput = document.getElementById("cameraInput");
    const galleryInput = document.getElementById("galleryInput");
    const filesInput = document.getElementById("filesInput");

    const selectedSummary = document.getElementById("selectedSummary");
    const uploadButton = document.getElementById("uploadButton");
    const uploadProgressWrap = document.getElementById("uploadProgressWrap");
    const uploadProgress = document.getElementById("uploadProgress");
    const uploadProgressText = document.getElementById("uploadProgressText");
    const uploadMessage = document.getElementById("uploadMessage");

    let selectedFiles = [];
    let eventSource = null;
    let lastClipboardFromServer = "";

    [cameraInput, galleryInput, filesInput].forEach(input => {
        input.addEventListener("change", () => {
            selectedFiles = Array.from(input.files || []);
            updateSelectionSummary();
        });
    });

    function updateSelectionSummary() {
        if (!selectedFiles.length) {
            selectedSummary.textContent = "Nic nie wybrano.";
            return;
        }

        const total = selectedFiles.reduce((sum, file) => sum + file.size, 0);

        selectedSummary.textContent =
            selectedFiles.length === 1
                ? `${selectedFiles[0].name} • ${formatBytes(total)}`
                : `${selectedFiles.length} plików • ${formatBytes(total)}`;
    }

    async function pair() {
        const pin = document.getElementById("pinInput").value.trim();
        const message = document.getElementById("pairMessage");

        message.className = "message";
        message.textContent = "Łączenie…";

        const response = await fetch("/api/pair", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                token: pairToken,
                pin
            })
        });

        if (!response.ok) {
            message.className = "message error";
            message.textContent = "Zły PIN albo nieaktualny kod QR.";
            return;
        }

        history.replaceState({}, "", "/");
        message.className = "message good";
        message.textContent = "Połączono.";

        await boot();
    }

    async function boot() {
        const response = await fetch("/api/state", {
            cache: "no-store"
        });

        if (response.status === 401) {
            appView.classList.add("hidden");
            pairView.classList.remove("hidden");
            return;
        }

        pairView.classList.add("hidden");
        appView.classList.remove("hidden");

        const state = await response.json();
        renderState(state);

        startRealtime();
    }

    function startRealtime() {
        if (eventSource)
            eventSource.close();

        eventSource = new EventSource("/events");

        const pill = document.getElementById("connectionPill");

        eventSource.onopen = () => {
            pill.className = "status-pill online";
            pill.textContent = "● Połączono";
        };

        eventSource.onmessage = async () => {
            await refreshState();
        };

        eventSource.onerror = () => {
            pill.className = "status-pill";
            pill.textContent = "Ponowne łączenie…";
        };
    }

    async function refreshState() {
        const response = await fetch("/api/state", {
            cache: "no-store"
        });

        if (response.status === 401) {
            location.reload();
            return;
        }

        if (!response.ok)
            return;

        const state = await response.json();
        renderState(state);
    }

    function renderState(state) {
        renderFiles(state.files || []);
        renderHistory(state.history || []);

        const clipboard = document.getElementById("clipboardText");

        if (document.activeElement !== clipboard &&
            state.clipboard !== lastClipboardFromServer) {
            clipboard.value = state.clipboard || "";
        }

        lastClipboardFromServer = state.clipboard || "";
    }

    function renderFiles(files) {
        const list = document.getElementById("downloadList");

        if (!files.length) {
            list.innerHTML =
                `<div class="message">Na komputerze nie udostępniono jeszcze plików.</div>`;
            return;
        }

        list.innerHTML = files.map(file => `
            <div class="file-row">
                <div>
                    <div class="file-name">${escapeHtml(file.name)}</div>
                    <div class="file-meta">${formatBytes(file.size)}</div>
                    <div id="downloadProgress_${file.id}" class="progress-wrap">
                        <div class="progress-track">
                            <div id="downloadBar_${file.id}" class="progress-bar"></div>
                        </div>
                        <div id="downloadText_${file.id}" class="progress-text">0%</div>
                    </div>
                </div>

                <button
                    class="download-btn"
                    onclick="downloadFile('${file.id}', '${escapeJs(file.name)}')">
                    Pobierz
                </button>
            </div>
        `).join("");
    }

    function renderHistory(items) {
        const list = document.getElementById("historyList");

        if (!items.length) {
            list.innerHTML = `<div class="message">Brak transferów w tej sesji.</div>`;
            return;
        }

        list.innerHTML = items.map(item => {
            const direction =
                item.direction === "PhoneToPc"
                    ? "iPhone → PC"
                    : "PC → iPhone";

            return `
                <div class="history-row">
                    <div class="history-main">
                        <div class="history-name">${escapeHtml(item.fileName)}</div>
                        <div>${escapeHtml(item.status)}</div>
                    </div>
                    <div class="history-meta">
                        ${direction} • ${formatBytes(item.size)} • ${formatTime(item.time)}
                    </div>
                </div>
            `;
        }).join("");
    }

    function uploadSelected() {
        if (!selectedFiles.length) {
            uploadMessage.className = "message error";
            uploadMessage.textContent = "Najpierw wybierz pliki.";
            return;
        }

        const formData = new FormData();

        for (const file of selectedFiles)
            formData.append("files", file);

        const xhr = new XMLHttpRequest();
        xhr.open("POST", "/upload");

        uploadButton.disabled = true;
        uploadProgressWrap.style.display = "block";
        uploadProgress.style.width = "0%";
        uploadProgressText.textContent = "0%";

        uploadMessage.className = "message";
        uploadMessage.textContent = "Wysyłanie…";

        xhr.upload.onprogress = event => {
            if (!event.lengthComputable)
                return;

            const percent = Math.round(event.loaded / event.total * 100);

            uploadProgress.style.width = `${percent}%`;
            uploadProgressText.textContent =
                `${percent}% • ${formatBytes(event.loaded)} / ${formatBytes(event.total)}`;
        };

        xhr.onload = () => {
            uploadButton.disabled = false;

            if (xhr.status >= 200 && xhr.status < 300) {
                uploadProgress.style.width = "100%";
                uploadProgressText.textContent = "100%";

                uploadMessage.className = "message good";
                uploadMessage.textContent = "Transfer zakończony.";

                selectedFiles = [];
                cameraInput.value = "";
                galleryInput.value = "";
                filesInput.value = "";
                updateSelectionSummary();
            }
            else if (xhr.status === 401) {
                location.reload();
            }
            else {
                uploadMessage.className = "message error";
                uploadMessage.textContent = "Nie udało się wysłać plików.";
            }
        };

        xhr.onerror = () => {
            uploadButton.disabled = false;
            uploadMessage.className = "message error";
            uploadMessage.textContent = "Utracono połączenie z komputerem.";
        };

        xhr.send(formData);
    }

    function downloadFile(id, fileName) {
        const wrap = document.getElementById(`downloadProgress_${id}`);
        const bar = document.getElementById(`downloadBar_${id}`);
        const text = document.getElementById(`downloadText_${id}`);

        wrap.style.display = "block";

        const xhr = new XMLHttpRequest();
        xhr.open("GET", `/download/${id}`);
        xhr.responseType = "blob";

        xhr.onprogress = event => {
            if (!event.lengthComputable) {
                text.textContent = formatBytes(event.loaded);
                return;
            }

            const percent = Math.round(event.loaded / event.total * 100);

            bar.style.width = `${percent}%`;
            text.textContent =
                `${percent}% • ${formatBytes(event.loaded)} / ${formatBytes(event.total)}`;
        };

        xhr.onload = () => {
            if (xhr.status === 200) {
                bar.style.width = "100%";
                text.textContent = "100%";

                const url = URL.createObjectURL(xhr.response);
                const link = document.createElement("a");

                link.href = url;
                link.download = fileName;

                document.body.appendChild(link);
                link.click();
                link.remove();

                setTimeout(() => URL.revokeObjectURL(url), 30000);
            }
            else if (xhr.status === 401) {
                location.reload();
            }
            else {
                text.textContent = "Błąd pobierania";
            }
        };

        xhr.onerror = () => {
            text.textContent = "Utracono połączenie";
        };

        xhr.send();
    }

    async function saveClipboard() {
        const text = document.getElementById("clipboardText").value;
        const message = document.getElementById("clipboardMessage");

        const response = await fetch("/api/clipboard", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ text })
        });

        if (response.ok) {
            message.className = "message good";
            message.textContent = "Schowek zaktualizowany.";
        }
        else {
            message.className = "message error";
            message.textContent = "Nie udało się zaktualizować schowka.";
        }
    }

    async function copyClipboard() {
        const textarea = document.getElementById("clipboardText");
        const message = document.getElementById("clipboardMessage");

        try {
            await navigator.clipboard.writeText(textarea.value);
            message.className = "message good";
            message.textContent = "Skopiowano.";
        }
        catch {
            textarea.focus();
            textarea.select();

            try {
                document.execCommand("copy");
                message.className = "message good";
                message.textContent = "Skopiowano.";
            }
            catch {
                message.className = "message error";
                message.textContent = "Safari zablokowało dostęp do schowka.";
            }
        }
    }

    function formatBytes(bytes) {
        if (!bytes)
            return "0 B";

        const units = ["B", "KB", "MB", "GB", "TB"];
        const index = Math.min(
            Math.floor(Math.log(bytes) / Math.log(1024)),
            units.length - 1);

        const value = bytes / Math.pow(1024, index);

        return `${value.toFixed(index === 0 ? 0 : 1)} ${units[index]}`;
    }

    function formatTime(value) {
        const date = new Date(value);

        return date.toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit"
        });
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function escapeJs(value) {
        return String(value)
            .replaceAll("\\", "\\\\")
            .replaceAll("'", "\\'");
    }


    function configureInstallExperience() {
        const standalone =
            window.matchMedia("(display-mode: standalone)").matches ||
            window.navigator.standalone === true;

        const isIos =
            /iphone|ipad|ipod/i.test(navigator.userAgent);

        if (isIos && !standalone) {
            document.getElementById("installHint").style.display = "block";
        }

        if ("serviceWorker" in navigator && window.isSecureContext) {
            navigator.serviceWorker
                .register("/sw.js")
                .catch(() => {});
        }
    }

    configureInstallExperience();

    boot();
</script>

</body>
</html>
""";
}
