using Serilog.Core;
using Serilog.Events;

namespace UniDesk.Web.Logging;

public class ThreadIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var property = propertyFactory.CreateProperty("ThreadId", Environment.CurrentManagedThreadId);
        logEvent.AddPropertyIfAbsent(property);
    }
}
