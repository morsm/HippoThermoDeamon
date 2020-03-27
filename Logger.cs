using System;
namespace Termors.Serivces.HippotronicsLedDaemon
{
    public sealed class Logger
    {
        private Logger()
        {
        }

        public static void Log(string message, params object[] args)
        {
            Console.WriteLine(FormatMessage(message, args));
        }

        public static void LogError(string message, params object[] args)
        {
            Console.Error.WriteLine(FormatMessage(message, args));
        }

        private static string FormatMessage(string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);

            return string.Format("{0}: {1}", DateTime.Now, formattedMessage);
        }
    }
}
