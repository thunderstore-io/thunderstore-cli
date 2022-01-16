using System.Text.Json.Serialization;

namespace ThunderstoreCLI.Models.Interaction;

public enum InteractionOutputType
{
    HUMAN,
    JSON,
}

public static class InteractionOptions
{
    public static InteractionOutputType OutputType { get; set; } = InteractionOutputType.HUMAN;
}

public abstract class BaseInteraction<T, Context> : BaseJson<T, Context>
    where T : BaseInteraction<T, Context>
    where Context : JsonSerializerContext
{
    public abstract string GetHumanString();

    public string GetString()
    {
        switch (InteractionOptions.OutputType)
        {
            case InteractionOutputType.HUMAN:
                return GetHumanString();
            case InteractionOutputType.JSON:
                return Serialize();
            default:
                throw new NotSupportedException();
        }
    }

    public void Write()
    {
        Console.WriteLine(GetString());
    }
}
