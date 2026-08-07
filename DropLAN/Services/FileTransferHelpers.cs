using System.IO;
namespace DropLAN.Services;

public static class FileTransferHelpers
{
    public static string SanitizeFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);

        foreach (var invalid in Path.GetInvalidFileNameChars())
            safe = safe.Replace(invalid, '_');

        return safe.Trim();
    }

    public static string GetUniqueFilePath(
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
}
