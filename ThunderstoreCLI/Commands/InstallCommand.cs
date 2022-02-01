using ThunderstoreCLI.Game;
using ThunderstoreCLI.Plugins;

namespace ThunderstoreCLI.Commands;

public static class InstallCommand
{
    public static int Run(Config.Config config)
    {
        throw new NotImplementedException();
    }

    public static int InstallLoader(Config.Config config)
    {
        var managerTypes = PluginManager.GetAllOfType<ModManager>();

        throw new NotImplementedException();
    }
}
