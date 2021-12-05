using System.Reflection;
using System.Text.RegularExpressions;

namespace ThunderstoreCLI;

public static class MiscUtils
{
    /// <summary>Return application version</summary>
    /// Version number is controlled via MinVer by creating new tags
    /// in git. See README for more information.
    public static int[] GetCurrentVersion()
    {
        string version;

        try
        {
            version = Assembly.GetEntryAssembly()!
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;
        }
        catch (NullReferenceException)
        {
            throw new Exception("Reading app version from assembly failed");
        }

        // Drop possible pre-release cruft ("-alpha.0.1") from the end.
        var versionParts = version.Split('-')[0].Split('.');

        if (versionParts is null || versionParts.Length != 3)
        {
            throw new Exception("Malformed app version: ${version}");
        }

        return versionParts.Select(part => Int32.Parse(part)).ToArray();
    }

    /// <summary>Extract version from release information</summary>
    /// <exception cref="ArgumentException">Throw if version number not found</exception>
    public static int[] ParseLatestVersion(string releaseJsonData)
    {
        var regex = new Regex(@"""tag_name"":""(\d+.\d+.\d+)""");
        MatchCollection matches = regex.Matches(releaseJsonData);

        if (matches.Count == 0)
        {
            throw new ArgumentException("Response didn't contain a valid release value");
        }

        return matches
            .Select(match => match.Groups[1].ToString().Split('.'))
            .Select(ver => ver.Select(part => Int32.Parse(part)).ToArray())
            .OrderByDescending(ver => ver, new Comparers.SemVer())
            .First();
    }

    /// <summary>Read information about releases from GitHub</summary>
    /// <exception cref="HttpRequestException">Throw for non-success status code</exception>
    /// <exception cref="TaskCanceledException">Throw if request timeouts</exception>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public static async Task<string> FetchReleaseInformation()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        client.DefaultRequestHeaders.Add("User-Agent", Defaults.GITHUB_USER);

        var url = $"https://api.github.com/repos/{Defaults.GITHUB_USER}/{Defaults.GITHUB_REPO}/releases";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
