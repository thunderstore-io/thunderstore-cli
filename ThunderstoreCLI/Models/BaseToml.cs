using System.Diagnostics.CodeAnalysis;
using Tomlet;

namespace ThunderstoreCLI.Models;

public abstract class BaseToml<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : ISerialize<T>
    where T : BaseToml<T>
{
    public string Serialize() => TomletMain.TomlStringFrom(this);

    public static T? Deserialize(string toml) => TomletMain.To<T>(toml);

    public static ValueTask<T?> DeserializeAsync(string toml) => new(Deserialize(toml));
    public static async ValueTask<T?> DeserializeAsync(Stream toml)
    {
        using StreamReader reader = new(toml);
        return Deserialize(await reader.ReadToEndAsync());
    }
}
