using System.IO;
using DropLAN;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class DownloadRoutes
{
    public static void MapDownloadRoutes(
        this WebApplication app,
        SharedState state,
        PairingSession session)
    {
        app.MapGet("/download/{id:guid}", async context =>
        {
            if (!session.IsAuthorized(context))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;
                return;
            }

            var idText = context.Request
                .RouteValues["id"]?
                .ToString();

            if (!Guid.TryParse(idText, out var id))
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;
                return;
            }

            var sharedFile = state.FindSharedFile(id);

            if (sharedFile is null)
            {
                context.Response.StatusCode =
                    StatusCodes.Status404NotFound;
                return;
            }

            // Kopiujemy właściwości do lokalnych, nienullowalnych zmiennych.
            // Dzięki temu analizator nullable nie gubi informacji w async lambda.
            var filePath = sharedFile.Path;
            var fileName = sharedFile.Name;
            var fileSize = sharedFile.Size;

            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                context.Response.StatusCode =
                    StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType =
                "application/octet-stream";
            context.Response.ContentLength = fileSize;
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.ContentDisposition =
                $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";

            try
            {
                await context.Response.SendFileAsync(
                    filePath,
                    context.RequestAborted);

                state.AddHistory(
                    fileName,
                    fileSize,
                    TransferDirection.PcToPhone,
                    "Zakończono");
            }
            catch (OperationCanceledException)
            {
                state.AddHistory(
                    fileName,
                    fileSize,
                    TransferDirection.PcToPhone,
                    "Anulowano");
            }
        });
    }
}
