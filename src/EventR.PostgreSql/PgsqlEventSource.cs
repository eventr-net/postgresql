namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using System.Diagnostics.Tracing;

    public class PgsqlEventSource : EventSource
    {
        public static readonly PgsqlEventSource Log = new PgsqlEventSource();

        [Event(
            EventIds.InitializationDone,
            Level = EventLevel.Verbose,
            Message = "Finished commits table initialization",
            Keywords = Keywords.Persistence)]
        public void InitializationDone()
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.InitializationDone);
            }
        }

        internal static class EventIds
        {
            public const int InitializationDone = 1;
        }
    }
}
