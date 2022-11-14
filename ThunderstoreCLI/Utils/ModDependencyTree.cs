using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Utils;

public static class ModDependencyTree
{
    public static IEnumerable<PackageListingV1> Generate(Config config, HttpClient http, PackageManifestV1 root)
    {
        var cachePath = Path.Combine(config.GeneralConfig.TcliConfig, "package-ror2.json");
        string packagesJson;
        if (!File.Exists(cachePath) || new FileInfo(cachePath).LastWriteTime.AddMinutes(5) < DateTime.Now)
        {
            var packageResponse = http.Send(config.Api.GetPackagesV1());
            packageResponse.EnsureSuccessStatusCode();
            using var responseReader = new StreamReader(packageResponse.Content.ReadAsStream());
            packagesJson = responseReader.ReadToEnd();
            File.WriteAllText(cachePath, packagesJson);
        }
        else
        {
            packagesJson = File.ReadAllText(cachePath);
        }

        var packages = PackageListingV1.DeserializeList(packagesJson)!;

        HashSet<string> visited = new();
        foreach (var originalDep in root.Dependencies!)
        {
            var match = InstallCommand.FullPackageNameRegex().Match(originalDep);
            var fullname = match.Groups["fullname"].Value;
            var depPackage = packages.Find(p => p.Fullname == fullname) ?? AttemptResolveExperimental(config, http, match, root.FullName);
            if (depPackage == null)
            {
                continue;
            }
            foreach (var dependency in GenerateInner(packages, config, http, depPackage, p => visited.Contains(p.Fullname!)))
            {
                // can happen on cycles, oh well
                if (visited.Contains(dependency.Fullname!))
                {
                    continue;
                }
                visited.Add(dependency.Fullname!);
                yield return dependency;
            }
        }
    }

    private static IEnumerable<PackageListingV1> GenerateInner(List<PackageListingV1> packages, Config config, HttpClient http, PackageListingV1 root, Predicate<PackageListingV1> visited)
    {
        if (visited(root))
        {
            yield break;
        }

        foreach (var dependency in root.Versions!.First().Dependencies!)
        {
            var match = InstallCommand.FullPackageNameRegex().Match(dependency);
            var fullname = match.Groups["fullname"].Value;
            var package = packages.Find(p => p.Fullname == fullname) ?? AttemptResolveExperimental(config, http, match, root.Fullname!);
            if (package == null)
            {
                continue;
            }
            foreach (var innerPackage in GenerateInner(packages, config, http, package, visited))
            {
                yield return innerPackage;
            }
        }

        yield return root;
    }

    private static PackageListingV1? AttemptResolveExperimental(Config config, HttpClient http, Match nameMatch, string neededBy)
    {
        var response = http.Send(config.Api.GetPackageMetadata(nameMatch.Groups["namespace"].Value, nameMatch.Groups["name"].Value));
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Write.Warn($"Failed to resolve dependency {nameMatch.Groups["fullname"].Value} for {neededBy}, continuing without it.");
            return null;
        }
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(response.Content.ReadAsStream());
        var data = PackageData.Deserialize(reader.ReadToEnd());

        Write.Warn($"Package {data!.Fullname} (needed by {neededBy}) exists in different community, ignoring");
        return null;
    }
}
