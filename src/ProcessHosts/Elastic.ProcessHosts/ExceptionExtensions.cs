using System;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts
{
	public static class ExceptionExtensions
	{
		public static void ToConsole(this Exception e, string prefix)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			var startUpException = e as StartupException;
			if (startUpException != null)
				Console.Error.WriteLine(e.Message);
			else
				Console.Error.WriteLine($"{prefix}: {e}");
			Console.ResetColor();
			if (!string.IsNullOrWhiteSpace(startUpException?.HelpText))
				Console.WriteLine(startUpException.HelpText);
		}
	}
}
