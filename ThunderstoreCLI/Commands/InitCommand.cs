using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class InitCommand
{
    public static int Run(Config config)
    {
        try
        {
            ValidateConfig(config);
        }
        catch (CommandException)
        {
            return 1;
        }

        var path = config.GetProjectConfigPath();
        var projectDir = Path.GetDirectoryName(path);
        if (projectDir is not null && !Directory.Exists(projectDir))
        {
            Write.Line($"Creating directory {projectDir}");
            Directory.CreateDirectory(projectDir);
        }

        Write.Line($"Creating a new project configuration to {projectDir}");
        if (File.Exists(path) && !config.InitConfig.ShouldOverwrite())
        {
            Write.Line($"Project configuration already exists, stopping");
            Write.Line($"Use the --{InitOptions.OVERWRITE_FLAG} to overwrite the file");
            return 1;
        }
        else
        {
            if (File.Exists(path))
            {
                Write.Line($"Project configuration already exists, overwriting");
            }
            File.WriteAllText(path, new ThunderstoreProject(Config.DefaultConfig).Serialize());

            var iconPath = config.GetPackageIconPath();
            if (File.Exists(iconPath))
            {
                Write.Line("Icon found, skipping creation of default");
            }
            else
            {
                File.WriteAllBytes(iconPath, Properties.Resources.icon);
            }

            var readmePath = config.GetPackageReadmePath();
            if (File.Exists(readmePath))
            {
                Write.Line("Readme found, skipping creation of default");
            }
            else
            {
                File.WriteAllText(readmePath, BuildReadme(config));
            }

            Write.Line("Done!");
            return 0;
        }
    }

    public static string BuildReadme(Config config)
    {
        return $@"
# {config.PackageConfig.Namespace}-{config.PackageConfig.Name}

{config.PackageConfig.Description}
".Trim();
    }

    private static void ValidateConfig(Config config)
    {
        var v = new CommandValidator("init");
        v.AddIfEmpty(config.PackageConfig.Namespace, "Package Namespace");
        v.AddIfEmpty(config.PackageConfig.Name, "Package Name");
        v.AddIfNotSemver(config.PackageConfig.VersionNumber, "Package VersionNumber");
        v.AddIfNull(config.PackageConfig.Description, "Package Description");
        v.AddIfNull(config.PackageConfig.WebsiteUrl, "Package WebsiteUrl");
        v.AddIfNull(config.PackageConfig.ContainsNsfwContent, "Package ContainsNsfwContent");
        v.AddIfNull(config.PackageConfig.Dependencies, "Package Dependencies");
        v.AddIfEmpty(config.BuildConfig.IconPath, "Build IconPath");
        v.AddIfEmpty(config.BuildConfig.ReadmePath, "Build ReadmePath");
        v.AddIfEmpty(config.BuildConfig.OutDir, "Build OutDir");
        v.AddIfEmpty(config.GeneralConfig.Repository, "Publish Repository");
        v.ThrowIfErrors();
    }
}
