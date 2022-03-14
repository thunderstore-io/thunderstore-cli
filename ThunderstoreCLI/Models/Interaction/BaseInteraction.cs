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

public abstract class BaseInteraction<T> : BaseJson<T>
    where T : BaseInteraction<T>
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
