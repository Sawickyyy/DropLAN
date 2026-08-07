using DropLAN.Routes;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace DropLAN;

public sealed class LocalServer
{
    public const int Port = 5050;

    private readonly SharedState _state;
    private readonly RealtimeBroker _realtime;
    private readonly PairingSession _session;

    private WebApplication? _app;

    public LocalServer(
        SharedState state,
        RealtimeBroker realtime)
    {
        _state = state;
        _realtime = realtime;
        _session = new PairingSession(realtime);

        _state.Changed += HandleStateChanged;
    }

    public string PairPin => _session.Pin;

    public string GetPairUrl()
    {
        var ip = NetworkHelper.GetLocalIPv4();
        return _session.GetPairUrl(ip, Port);
    }

    public void RegenerateSession()
    {
        _session.Regenerate();
    }

    public async Task StartAsync()
    {
        if (_app != null)
            return;

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls(
            $"http://0.0.0.0:{Port}");

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

        var app = builder.Build();
        _app = app;

        var webAssetsPath = Path.Combine(
            AppContext.BaseDirectory,
            "WebAssets");

        app.MapPwaRoutes(webAssetsPath);
        app.MapPairRoutes(_session);
        app.MapStateRoutes(
            _state,
            _realtime,
            _session);
        app.MapEventRoutes(
            _realtime,
            _session);
        app.MapUploadRoutes(
            _state,
            _session);
        app.MapDownloadRoutes(
            _state,
            _session);
        app.MapClipboardRoutes(
            _state,
            _session);

        await app.StartAsync();
    }

    public async Task StopAsync()
    {
        var app = _app;

        if (app == null)
            return;

        _app = null;

        try
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
        finally
        {
            // Celowo pusto. _app jest już wyzerowane,
            // więc kolejne StartAsync może wystartować serwer ponownie.
        }
    }

    private void HandleStateChanged()
    {
        _realtime.Publish();
    }
}
