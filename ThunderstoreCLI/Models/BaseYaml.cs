using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace ThunderstoreCLI.Models;

public abstract class BaseYaml<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : ISerialize<T> where T : BaseYaml<T>
{
    public string Serialize()
    {
        return BaseYamlHelper.Serializer.Serialize(this);
    }
    public static T? Deserialize(string input)
    {
        return BaseYamlHelper.Deserializer.Deserialize<T>(input);
    }
}

file static class BaseYamlHelper
{
    public static readonly Serializer Serializer = new();
    public static readonly Deserializer Deserializer = new();
}
