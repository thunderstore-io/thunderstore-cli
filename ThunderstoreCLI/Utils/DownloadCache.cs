namespace ThunderstoreCLI.Utils;

public sealed class DownloadCache
{
    public string CacheDirectory { get; }

    private HttpClient Client { get; } = new()
    {
        Timeout = TimeSpan.FromHours(1)
    };

    public DownloadCache(string cacheDirectory)
    {
        CacheDirectory = cacheDirectory;
    }

    // Task instead of ValueTask here because these Tasks will be await'd multiple times (ValueTask does not allow that)
    public Task<string> GetFileOrDownload(string filename, string downloadUrl)
    {
        string fullPath = Path.Combine(CacheDirectory, filename);
        if (File.Exists(fullPath))
        {
            return Task.FromResult(fullPath);
        }

        return DownloadFile(fullPath, downloadUrl);
    }

    private async Task<string> DownloadFile(string fullpath, string downloadUrl)
    {
        var tempPath = fullpath + ".tmp";

        await ActionUtils.RetryAsync(5, async () =>
        {
            // copy into memory first to prevent canceled downloads creating files on the disk
            await using FileStream tempStream = new(tempPath, FileMode.Create, FileAccess.Write);
            await using var downloadStream = await Client.GetStreamAsync(downloadUrl);
            await downloadStream.CopyToAsync(tempStream);
        });

        File.Move(tempPath, fullpath);
        return fullpath;
    }
}
