using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Process;

namespace Elastic.Installer.Domain.Extensions
{
	public static class ExceptionExtensions
	{
		public static void ToConsole(this Exception e, string prefix)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			if (e is StartupException)
				Console.Error.WriteLine(e.Message);
			else
				Console.Error.WriteLine($"{prefix}: {e}");
			Console.ResetColor();
		}

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
