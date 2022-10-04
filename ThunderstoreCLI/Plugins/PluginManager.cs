using ThunderstoreCLI.Configuration;

namespace ThunderstoreCLI.Plugins;

public class PluginManager
{
    private class Plugin
    {

    }

    private class PluginInfo
    {

    }

    public string PluginDirectory { get; }

    private List<Plugin> LoadedPlugins = new();

    public PluginManager(GeneralConfig config)
    {
        PluginDirectory = Path.Combine(config.TcliConfig, "Plugins");
    }
}
