namespace DropLAN;

public static class UpdateSettings
{
    // ZMIEŃ po utworzeniu repozytorium GitHub, np.
    // https://github.com/Sawickyyy/DropLAN
    public const string GitHubRepositoryUrl =
        "https://github.com/Sawickyyy/DropLAN";

    public static bool IsConfigured =>
        !GitHubRepositoryUrl.Contains(
            "Sawickyyy",
            StringComparison.OrdinalIgnoreCase);
}
