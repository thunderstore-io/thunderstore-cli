using ThunderstoreCLI.Config;
using ThunderstoreCLI.Options;

namespace ThunderstoreCLI.Commands;

public static class InitCommand
{
    public static int Run(Config.Config config)
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
            ProjectFileConfig.Write(config, path);

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

    public static string BuildReadme(Config.Config config)
    {
        return $@"
# {config.PackageMeta.Namespace}-{config.PackageMeta.Name}

{config.PackageMeta.Description}
".Trim();
    }

    private static void ValidateConfig(Config.Config config)
    {
        var v = new Config.Validator("init");
        v.AddIfEmpty(config.PackageMeta.Namespace, "Package Namespace");
        v.AddIfEmpty(config.PackageMeta.Name, "Package Name");
        v.AddIfNotSemver(config.PackageMeta.VersionNumber, "Package VersionNumber");
        v.AddIfNull(config.PackageMeta.Description, "Package Description");
        v.AddIfNull(config.PackageMeta.WebsiteUrl, "Package WebsiteUrl");
        v.AddIfNull(config.PackageMeta.ContainsNsfwContent, "Package ContainsNsfwContent");
        v.AddIfNull(config.PackageMeta.Dependencies, "Package Dependencies");
        v.AddIfEmpty(config.BuildConfig.IconPath, "Build IconPath");
        v.AddIfEmpty(config.BuildConfig.ReadmePath, "Build ReadmePath");
        v.AddIfEmpty(config.BuildConfig.OutDir, "Build OutDir");
        v.AddIfEmpty(config.PublishConfig.Repository, "Publish Repository");
        v.ThrowIfErrors();
    }
}
