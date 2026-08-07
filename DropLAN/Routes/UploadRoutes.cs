using System.IO;
using DropLAN;
using DropLAN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class UploadRoutes
{
    public static void MapUploadRoutes(
        this WebApplication app,
        SharedState state,
        PairingSession session)
    {
        app.MapPost("/upload", async context =>
        {
            if (!session.IsAuthorized(context))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;
                return;
            }

            if (!context.Request.HasFormContentType)
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;
                return;
            }

            var form = await context.Request.ReadFormAsync(
                context.RequestAborted);

            if (form.Files.Count == 0)
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;
                return;
            }

            var targetFolder = state.DownloadFolder;
            Directory.CreateDirectory(targetFolder);

            var saved = new List<object>();

            foreach (var file in form.Files)
            {
                if (file.Length <= 0)
                    continue;

                var safeName = FileTransferHelpers
                    .SanitizeFileName(file.FileName);

                if (string.IsNullOrWhiteSpace(safeName))
                    continue;

                var destination = FileTransferHelpers
                    .GetUniqueFilePath(
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

                    state.AddHistory(
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
                    state.AddHistory(
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
}
