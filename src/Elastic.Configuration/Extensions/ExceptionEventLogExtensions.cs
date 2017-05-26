using System;
using System.Diagnostics;
using System.Security;

namespace Elastic.Configuration.Extensions
{
	public static class ExceptionEventLogExtensions
	{
		public static void ToEventLog(this Exception e, string source)
		{
			try
			{
				if (!EventLog.SourceExists(source))
					EventLog.CreateEventSource(source, "Application");
				EventLog.WriteEntry(source, e.ToString(), EventLogEntryType.Error);
			}
			catch (SecurityException)
			{
				Console.Error.WriteLine("Elasticsearch.exe does not have permission to write exception to event log");
			}
		}
	}
}