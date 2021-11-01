using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThunderstoreCLI
{
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
            var version = regex.Match(releaseJsonData);

            if (version is null)
            {
                throw new ArgumentException("Response didn't contain a valid release value");
            }

            var parts = version.Groups[1].ToString().Split('.');
            return parts.Select(part => Int32.Parse(part)).ToArray();
        }

        /// <summary>Read information about latest release from GitHub</summary>
        /// <exception cref="HttpRequestException">Throw for non-success status code</exception>
        /// <exception cref="TaskCanceledException">Throw if request timeouts</exception>
        public static async Task<string> FetchLatestReleaseInformation()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.DefaultRequestHeaders.Add("User-Agent", Defaults.GITHUB_USER);

            var url = $"https://api.github.com/repos/{Defaults.GITHUB_USER}/{Defaults.GITHUB_REPO}/releases/latest";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
