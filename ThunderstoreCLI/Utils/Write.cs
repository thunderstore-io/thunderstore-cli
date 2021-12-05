using static Crayon.Output;

namespace ThunderstoreCLI;

public static class Write
{
    private static void _Error(string msg) => Console.WriteLine(Red(msg));
    private static void _Light(string msg) => Console.WriteLine(Dim(msg));
    private static void _Regular(string msg) => Console.WriteLine(msg);
    private static void _Success(string msg) => Console.WriteLine(Green(msg));
    private static void _Warn(string msg) => Console.WriteLine(Yellow(msg));

    private static void _WriteMultiline(Action<string> write, string msg, string[] submsgs)
    {
        write(msg);
        submsgs.ToList().ForEach(write);
    }

    /// <summary>Write empty line to stdout</summary>
    public static void Empty() => _Regular("");

    /// <summary>Write error message to stdout</summary>
    public static void Error(string message, params string[] submessages)
    {
        _WriteMultiline(_Error, $"ERROR: {message}", submessages);
    }

    /// <summary>Write error message with note about exiting to stdout</summary>
    public static void ErrorExit(string message, params string[] submessages)
    {
        Error(message, submessages);
        _Error("Exiting");
    }

    /// <summary>Write line with underlining to stdout</summary>
    public static void Header(string header)
    {
        Empty();
        _Regular(header);
        _Regular(new string('-', header.Length));
    }

    /// <summary>Write message with dimmer color to stdout</summary>
    public static void Light(string message, params string[] submessages)
    {
        _WriteMultiline(_Light, message, submessages);
    }

    /// <summary>Write regular line to stdout</summary>
    public static void Line(string message) => _Regular(message);

    /// <summary>Write message with highlight color to stdout</summary>
    public static void Note(string message, params string[] submessages)
    {
        _WriteMultiline(_Warn, message, submessages);
    }

    /// <summary>Write success message to stdout</summary>
    public static void Success(string message, params string[] submessages)
    {
        _WriteMultiline(_Success, message, submessages);
    }

    /// <summary>Write warning message to stdout</summary>
    public static void Warn(string message, params string[] submessages)
    {
        _WriteMultiline(_Warn, $"WARNING: {message}", submessages);
    }

    /// <summary>Output line to stdout with optional empty lines before and/or after</summary>
    public static void WithNL(string message, bool before = false, bool after = false)
    {
        if (before)
        {
            Empty();
        }

        _Regular(message);

        if (after)
        {
            Empty();
        }
    }
}
