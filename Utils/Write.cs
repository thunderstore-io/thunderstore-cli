using System;
using System.Linq;
using static Crayon.Output;

namespace ThunderstoreCLI
{
    public static class Write
    {
        private static void _Error(string msg) => Console.WriteLine(Red(msg));
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
    }
}
