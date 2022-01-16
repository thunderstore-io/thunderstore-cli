namespace ThunderstoreCLI;

public static class UpdateChecker
{
    public static async Task<string> CheckForUpdates()
    {
        var current = MiscUtils.GetCurrentVersion();
        int[] latest;

        try
        {
            var responseContent = await MiscUtils.FetchReleaseInformation();
            latest = MiscUtils.ParseLatestVersion(responseContent);
        }
        catch (Exception)
        {
            return "";
        }

        if (
            latest[0] > current[0] ||
            (latest[0] == current[0] && latest[1] > current[1]) ||
            (latest[0] == current[0] && latest[1] == current[1] && latest[2] > current[2])
        )
        {
            var version = $"{latest[0]}.{latest[1]}.{latest[2]}";
            return $"Newer version {version} of Thunderstore CLI is available";
        }

        return "";
    }

    public static void WriteUpdateNotification(Task<string> checkTask)
    {
        if (!checkTask.IsCompleted)
        {
            return;
        }

        var notification = checkTask.GetAwaiter().GetResult();

        if (notification != "")
        {
            Write.Note(notification);
        }
    }
}
