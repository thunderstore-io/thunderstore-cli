using System.Diagnostics.CodeAnalysis;

namespace ThunderstoreCLI.Models;

public interface ISerialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    where T : ISerialize<T>
{
    public string Serialize();
#if NET7_0
    public static abstract T? Deserialize(string input);
    public static abstract ValueTask<T?> DeserializeAsync(string input);
    public static abstract ValueTask<T?> DeserializeAsync(Stream input);
#endif
}
