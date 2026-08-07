using System.IO;
namespace DropLAN;

public sealed class SharedState
{
    private readonly object _sync = new();
    private readonly List<SharedFileItem> _sharedFiles = [];
    private readonly List<TransferHistoryItem> _history = [];

    private string _clipboardText = "";
    private string _downloadFolder = SettingsStore.LoadDownloadFolder();

    public event Action? Changed;
    public event Action<TransferHistoryItem>? TransferAdded;

    public string DownloadFolder
    {
        get
        {
            lock (_sync)
                return _downloadFolder;
        }
    }

    public string ClipboardText
    {
        get
        {
            lock (_sync)
                return _clipboardText;
        }
    }

    public IReadOnlyList<SharedFileItem> GetSharedFiles()
    {
        lock (_sync)
            return _sharedFiles.ToList();
    }

    public IReadOnlyList<TransferHistoryItem> GetHistory()
    {
        lock (_sync)
            return _history.OrderByDescending(x => x.Time).Take(100).ToList();
    }

    public void SetDownloadFolder(string folder)
    {
        Directory.CreateDirectory(folder);

        lock (_sync)
            _downloadFolder = folder;

        SettingsStore.SaveDownloadFolder(folder);
        Changed?.Invoke();
    }

    public void AddSharedFiles(IEnumerable<string> paths)
    {
        var changed = false;

        lock (_sync)
        {
            foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(path))
                    continue;

                if (_sharedFiles.Any(x =>
                        string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var info = new FileInfo(path);

                _sharedFiles.Add(new SharedFileItem
                {
                    Path = info.FullName,
                    Name = info.Name,
                    Size = info.Length
                });

                changed = true;
            }
        }

        if (changed)
            Changed?.Invoke();
    }

    public void RemoveSharedFile(Guid id)
    {
        lock (_sync)
            _sharedFiles.RemoveAll(x => x.Id == id);

        Changed?.Invoke();
    }

    public void ClearSharedFiles()
    {
        lock (_sync)
            _sharedFiles.Clear();

        Changed?.Invoke();
    }

    public SharedFileItem? FindSharedFile(Guid id)
    {
        lock (_sync)
            return _sharedFiles.FirstOrDefault(x => x.Id == id);
    }

    public void SetClipboard(string text)
    {
        lock (_sync)
            _clipboardText = text ?? "";

        Changed?.Invoke();
    }

    public void AddHistory(
        string fileName,
        long size,
        TransferDirection direction,
        string status)
    {
        var item = new TransferHistoryItem
        {
            FileName = fileName,
            Size = size,
            Direction = direction,
            Status = status
        };

        lock (_sync)
        {
            _history.Add(item);

            if (_history.Count > 250)
                _history.RemoveRange(
                    0,
                    _history.Count - 250);
        }

        TransferAdded?.Invoke(item);
        Changed?.Invoke();
    }
}
