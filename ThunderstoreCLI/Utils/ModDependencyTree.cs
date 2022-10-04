using System.Collections.Concurrent;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Utils;

public static class ModDependencyTree
{
    public static IEnumerable<PackageData> Generate(Config config, HttpClient http, PackageManifestV1 root)
    {
        HashSet<string> alreadyGottenPackages = new();
        foreach (var dependency in root.Dependencies!)
        {
            var depParts = dependency.Split('-');
            var depRequest = http.Send(config.Api.GetPackageMetadata(depParts[0], depParts[1]));
            depRequest.EnsureSuccessStatusCode();
            var depData = PackageData.Deserialize(depRequest.Content.ReadAsStream());
            foreach (var package in GenerateInternal(config, http, depData!, package => alreadyGottenPackages.Contains(package.Fullname!)))
            {
                // this can happen on cyclical references, oh well
                if (alreadyGottenPackages.Contains(package.Fullname!))
                    continue;

                alreadyGottenPackages.Add(package.Fullname!);
                yield return package;
            }
        }
    }
    private static IEnumerable<PackageData> GenerateInternal(Config config, HttpClient http, PackageData root, Predicate<PackageData> alreadyGotten)
    {
        if (alreadyGotten(root))
            yield break;

        foreach (var dependency in root.LatestVersion!.Dependencies!)
        {
            var depParts = dependency.Split('-');
            var depRequest = http.Send(config.Api.GetPackageMetadata(depParts[0], depParts[1]));
            depRequest.EnsureSuccessStatusCode();
            var depData = PackageData.Deserialize(depRequest.Content.ReadAsStream());
            foreach (var package in GenerateInternal(config, http, depData!, alreadyGotten))
            {
                yield return package;
            }
        }
        yield return root;
    }
}
