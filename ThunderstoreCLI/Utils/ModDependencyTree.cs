using System.Net;
using System.Text.RegularExpressions;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Utils;

public static class ModDependencyTree
{
    public static IEnumerable<PackageVersionV1> Generate(Config config, HttpClient http, PackageManifestV1 root, string? sourceCommunity, bool useExactVersions)
    {
        List<PackageListingV1>? packages = null;

        if (sourceCommunity != null)
        {
            var cachePath = Path.Combine(config.GeneralConfig.TcliConfig, $"package-{sourceCommunity}.json");
            string packagesJson;
            if (!File.Exists(cachePath) || new FileInfo(cachePath).LastWriteTime.AddMinutes(5) < DateTime.Now)
            {
                var packageResponse = http.Send(config.Api.GetPackagesV1(sourceCommunity));
                packageResponse.EnsureSuccessStatusCode();
                using var responseReader = new StreamReader(packageResponse.Content.ReadAsStream());
                packagesJson = responseReader.ReadToEnd();
                File.WriteAllText(cachePath, packagesJson);
            }
            else
            {
                packagesJson = File.ReadAllText(cachePath);
            }

            packages = PackageListingV1.DeserializeList(packagesJson)!;
        }

        Queue<string> toVisit = new();
        Dictionary<string, (int id, PackageVersionV1 version)> dict = new();
        int currentId = 0;
        foreach (var dep in root.Dependencies!)
        {
            toVisit.Enqueue(dep);
        }
        while (toVisit.TryDequeue(out var packageString))
        {
            var match = InstallCommand.FullPackageNameRegex.Match(packageString);
            var fullname = match.Groups["fullname"].Value;
            if (dict.TryGetValue(fullname, out var current))
            {
                dict[fullname] = (currentId++, current.version);
                continue;
            }
            var package = packages?.Find(p => p.Fullname == fullname) ?? AttemptResolveExperimental(config, http, match);
            if (package is null)
                continue;
            PackageVersionV1? version;
            if (useExactVersions)
            {
                string requiredVersion = match.Groups["version"].Value;
                version = package.Versions!.FirstOrDefault(v => v.VersionNumber == requiredVersion);
                if (version is null)
                {
                    Write.Warn($"Version {requiredVersion} could not be found for mod {fullname}, using latest instead");
                    version = package.Versions!.First();
                }
            }
            else
            {
                version = package.Versions!.First();
            }
            dict[fullname] = (currentId++, version);
            foreach (var dep in version.Dependencies!)
            {
                toVisit.Enqueue(dep);
            }
        }
        return dict.Values.OrderByDescending(x => x.id).Select(x => x.version);
    }

    private static PackageListingV1? AttemptResolveExperimental(Config config, HttpClient http, Match nameMatch)
    {
        var response = http.Send(config.Api.GetPackageMetadata(nameMatch.Groups["namespace"].Value, nameMatch.Groups["name"].Value));
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Write.Warn($"Failed to resolve dependency {nameMatch.Groups["fullname"].Value}, continuing without it.");
            return null;
        }
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(response.Content.ReadAsStream());
        var data = PackageData.Deserialize(reader.ReadToEnd());

        Write.Warn($"Package {data!.Fullname} exists in different community, ignoring");
        return null;
    }
}
