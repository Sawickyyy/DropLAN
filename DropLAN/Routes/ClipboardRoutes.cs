using DropLAN;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class ClipboardRoutes
{
    public static void MapClipboardRoutes(
        this WebApplication app,
        SharedState state,
        PairingSession session)
    {
        app.MapPost("/api/clipboard", async context =>
        {
            if (!session.IsAuthorized(context))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;
                return;
            }

            var request = await context.Request
                .ReadFromJsonAsync<ClipboardRequest>(
                    cancellationToken: context.RequestAborted);

            if (request == null)
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;
                return;
            }

            state.SetClipboard(request.Text ?? "");

            await context.Response.WriteAsJsonAsync(
                new { ok = true },
                context.RequestAborted);
        });
    }
}
