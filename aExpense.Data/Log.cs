namespace AExpense.Data
{
    using System.Diagnostics;

    public static class Log
    {
        public static void Write(EventKind eventKind, string message, params object[] args)
        {
            switch (eventKind)
            {
                case EventKind.Error:
                case EventKind.Critical:
                    Trace.TraceError(message, args);
                    break;
                case EventKind.Warning:
                    Trace.TraceWarning(message, args);
                    break;
                case EventKind.Information:
                case EventKind.Verbose:
                    Trace.TraceInformation(message, args);
                    break;
            }
        }
    }
}