using Velopack;
using Velopack.Sources;

namespace DropLAN;

public sealed class UpdateService
{
    private UpdateManager? _manager;

    public bool IsConfigured => UpdateSettings.IsConfigured;

    private UpdateManager GetManager()
    {
        if (_manager != null)
            return _manager;

        if (!UpdateSettings.IsConfigured)
            throw new InvalidOperationException(
                "Najpierw ustaw adres repozytorium w UpdateSettings.cs.");

        _manager = new UpdateManager(
            new GithubSource(
                UpdateSettings.GitHubRepositoryUrl,
                accessToken: null,
                prerelease: false));

        return _manager;
    }

    public async Task<UpdateInfo?> CheckAsync()
    {
        return await GetManager().CheckForUpdatesAsync();
    }

    public async Task DownloadAndInstallAsync(
        UpdateInfo update,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var manager = GetManager();

        await manager.DownloadUpdatesAsync(
            update,
            progress,
            cancellationToken);

        manager.ApplyUpdatesAndRestart(update);
    }
}
