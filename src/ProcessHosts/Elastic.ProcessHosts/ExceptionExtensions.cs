using System;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts
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
	}
}
