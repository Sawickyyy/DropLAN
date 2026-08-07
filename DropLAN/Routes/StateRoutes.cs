using DropLAN;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class StateRoutes
{
    public static void MapStateRoutes(
        this WebApplication app,
        SharedState state,
        RealtimeBroker realtime,
        PairingSession session)
    {
        app.MapGet("/api/state", async context =>
        {
            if (!session.IsAuthorized(context))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;
                return;
            }

            var files = state.GetSharedFiles()
                .Select(file => new
                {
                    id = file.Id,
                    name = file.Name,
                    size = file.Size,
                    addedAt = file.AddedAt
                });

            var history = state.GetHistory()
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
                    clipboard = state.ClipboardText,
                    history,
                    connectedClients = realtime.ClientCount
                },
                context.RequestAborted);
        });
    }
}
