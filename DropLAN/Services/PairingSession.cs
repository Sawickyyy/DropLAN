using DropLAN;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace DropLAN.Services;

public sealed class PairingSession
{
    public const string CookieName = "DropLANSession";

    private readonly object _sync = new();
    private readonly RealtimeBroker _realtime;

    private string _token = "";
    private string _pin = "";

    public PairingSession(RealtimeBroker realtime)
    {
        _realtime = realtime;
        Regenerate();
    }

    public string Pin
    {
        get
        {
            lock (_sync)
                return _pin;
        }
    }

    public string GetPairUrl(string ip, int port)
    {
        lock (_sync)
            return $"http://{ip}:{port}/?token={Uri.EscapeDataString(_token)}";
    }

    public void Regenerate()
    {
        lock (_sync)
        {
            _token = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(24))
                .ToLowerInvariant();

            _pin = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();
        }

        _realtime.Publish("session");
    }

    public bool Validate(string? token, string? pin)
    {
        string expectedToken;
        string expectedPin;

        lock (_sync)
        {
            expectedToken = _token;
            expectedPin = _pin;
        }

        var tokenBytes = Encoding.UTF8.GetBytes(token ?? "");
        var expectedTokenBytes = Encoding.UTF8.GetBytes(expectedToken);

        var tokenOk = tokenBytes.Length == expectedTokenBytes.Length &&
                      CryptographicOperations.FixedTimeEquals(
                          tokenBytes,
                          expectedTokenBytes);

        var pinOk = string.Equals(
            pin,
            expectedPin,
            StringComparison.Ordinal);

        return tokenOk && pinOk;
    }

    public bool IsAuthorized(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(
                CookieName,
                out var cookie))
            return false;

        string expectedToken;

        lock (_sync)
            expectedToken = _token;

        var cookieBytes = Encoding.UTF8.GetBytes(cookie);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return cookieBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(
                   cookieBytes,
                   expectedBytes);
    }

    public string CurrentToken
    {
        get
        {
            lock (_sync)
                return _token;
        }
    }
}
