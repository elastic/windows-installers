namespace Elastic.ProcessHosts.Process
{
	public interface IConsoleOutHandler
	{
		void Handle(ConsoleOut consoleOut);
		void Write(ConsoleOut consoleOut);
	}
}