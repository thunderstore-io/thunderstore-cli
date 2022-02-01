using System.Diagnostics.CodeAnalysis;

namespace ThunderstoreCLI.Plugins;

public static class PluginManager
{
    public static List<Type> GetAllOfType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        return typeof(PluginManager).Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(T))).ToList();
    }
}
