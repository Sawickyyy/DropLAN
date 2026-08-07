namespace DropLAN;

public enum TransferDirection
{
    PhoneToPc,
    PcToPhone
}

public sealed class SharedFileItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Path { get; init; }
    public required string Name { get; init; }
    public long Size { get; init; }
    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.Now;

    public string DisplayLabel => $"{Name}  •  {FormatBytes(Size)}";

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{size:0} {units[unit]}"
            : $"{size:0.##} {units[unit]}";
    }
}

public sealed class TransferHistoryItem
{
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;
    public required string FileName { get; init; }
    public long Size { get; init; }
    public TransferDirection Direction { get; init; }
    public required string Status { get; init; }

    public string DisplayLabel
    {
        get
        {
            var arrow = Direction == TransferDirection.PhoneToPc ? "iPhone → PC" : "PC → iPhone";
            return $"{Time:HH:mm:ss}   {arrow}   {FileName}   •   {FormatBytes(Size)}   •   {Status}";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{size:0} {units[unit]}"
            : $"{size:0.##} {units[unit]}";
    }
}

public sealed record PairRequest(string Token, string Pin);
public sealed record ClipboardRequest(string Text);
