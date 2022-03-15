namespace ThunderstoreCLI.Plugins;

public static class PluginManager
{
    public static List<Type> GetAllOfType<T>()
    {
        return typeof(PluginManager).Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(T))).ToList();
    }
}
