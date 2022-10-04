namespace ThunderstoreCLI.Utils;

public sealed class CommandFatalException : Exception
{
    public string ErrorMessage { get; }
    public CommandFatalException(string errorMessage) : base(errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}
