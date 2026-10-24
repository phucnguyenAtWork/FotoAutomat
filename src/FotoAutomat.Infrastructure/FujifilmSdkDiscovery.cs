namespace FotoAutomat.Infrastructure;

public static class FujifilmSdkDiscovery
{
    public static string? FindInstalled(string? profile = null)
    {
        profile ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[] { Environment.GetEnvironmentVariable("FUJIFILM_SDK_DIR"),
            Path.Combine(profile, "Downloads", "SDK13410"), Path.Combine(AppContext.BaseDirectory, "FujifilmSDK") };
        return FindInCandidates(candidates);
    }
    public static string? FindInCandidates(IEnumerable<string?> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try { return FujifilmCameraConnection.ResolveDirectory(candidate); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { }
        }
        return null;
    }
}

