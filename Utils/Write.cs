using System;
using System.Linq;
using static Crayon.Output;

namespace ThunderstoreCLI
{
    public static class Write
    {
        private static void _Error(string msg) => Console.WriteLine(Red(msg));
        private static void _Warn(string msg) => Console.WriteLine(Yellow(msg));

        private static void _WriteMultiline(Action<string> write, string msg, string[] submsgs)
        {
            write(msg);
            submsgs.ToList().ForEach(write);
        }

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

        /// <summary>Write warning message to stdout</summary>
        public static void Warn(string message, params string[] submessages)
        {
            _WriteMultiline(_Warn, $"WARNING: {message}", submessages);
        }
    }
}
