using System.Diagnostics.CodeAnalysis;
using Tomlet;

namespace ThunderstoreCLI.Models;

public abstract class BaseToml<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : ISerialize<T>
    where T : BaseToml<T>
{
    public string Serialize() => TomletMain.TomlStringFrom(this);

    public static T Deserialize(string toml) => TomletMain.To<T>(toml);
}
