using DropLAN;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class EventRoutes
{
    public static void MapEventRoutes(
        this WebApplication app,
        RealtimeBroker realtime,
        PairingSession session)
    {
        app.MapGet("/events", async context =>
        {
            if (!session.IsAuthorized(context))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";
            context.Response.ContentType = "text/event-stream";

            var reader = realtime.Subscribe(
                context.RequestAborted);

            await context.Response.WriteAsync(
                "data: connected\n\n",
                context.RequestAborted);

            await context.Response.Body.FlushAsync(
                context.RequestAborted);

            try
            {
                await foreach (var message in reader
                    .ReadAllAsync(context.RequestAborted))
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
}
