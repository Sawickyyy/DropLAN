namespace DropLAN;

public static class UpdateSettings
{
    // ZMIEŃ po utworzeniu repozytorium GitHub, np.
    // https://github.com/AJgorEx/DropLAN
    public const string GitHubRepositoryUrl =
        "https://github.com/CHANGE_ME/DropLAN";

    public static bool IsConfigured =>
        !GitHubRepositoryUrl.Contains(
            "CHANGE_ME",
            StringComparison.OrdinalIgnoreCase);
}
