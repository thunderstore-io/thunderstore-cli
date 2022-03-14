using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace ThunderstoreCLI.Models;

public abstract class BaseJson<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    where T : BaseJson<T>
{
    public string Serialize(JsonSerializerSettings? options = null)
    {
        return JsonConvert.SerializeObject(this, options);
    }
    public static T? Deserialize(string json, JsonSerializerSettings? options = null)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
    public static async Task<T?> Deserialize(Stream json, JsonSerializerSettings? options = null)
    {
        using StreamReader reader = new(json);
        return Deserialize(await reader.ReadToEndAsync(), options);
    }
}
