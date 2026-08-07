using DropLAN;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class PairRoutes
{
    public static void MapPairRoutes(
        this WebApplication app,
        PairingSession session)
    {
        app.MapPost("/api/pair", async context =>
        {
            var request = await context.Request
                .ReadFromJsonAsync<PairRequest>(
                    cancellationToken: context.RequestAborted);

            if (request == null)
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;
                return;
            }

            if (!session.Validate(request.Token, request.Pin))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(
                    new { message = "Nieprawidłowy kod PIN." },
                    context.RequestAborted);

                return;
            }

            context.Response.Cookies.Append(
                PairingSession.CookieName,
                session.CurrentToken,
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

        app.MapPost("/api/logout", async context =>
        {
            context.Response.Cookies.Delete(
                PairingSession.CookieName);

            await context.Response.WriteAsJsonAsync(
                new { ok = true },
                context.RequestAborted);
        });
    }
}
