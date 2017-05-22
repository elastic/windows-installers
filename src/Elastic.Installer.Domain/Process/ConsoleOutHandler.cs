using System;

namespace Elastic.Installer.Domain.Process
{
	public class ConsoleOutHandler : IConsoleOutHandler
	{
		public void Handle(ConsoleOut consoleOut) { }

		public virtual void Write(ConsoleOut consoleOut)
		{
			if (consoleOut.Error)
				Console.Error.WriteLine(consoleOut.Data);
			else
				Console.WriteLine(consoleOut.Data);
		}
	}
}