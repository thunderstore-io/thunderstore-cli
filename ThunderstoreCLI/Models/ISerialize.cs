using System.Diagnostics.CodeAnalysis;

namespace ThunderstoreCLI.Models;

public interface ISerialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    where T : ISerialize<T>
{
    public string Serialize();
#if NET7_0
    public static abstract T? Deserialize(string input);
    public static virtual ValueTask<T?> DeserializeAsync(string input)
    {
        return new(T.Deserialize(input));
    }
    public static virtual async ValueTask<T?> DeserializeAsync(Stream input)
    {
        using StreamReader reader = new(input);
        return T.Deserialize(await reader.ReadToEndAsync());
    }
#endif
}
