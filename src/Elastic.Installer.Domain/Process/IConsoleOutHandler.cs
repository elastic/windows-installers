namespace Elastic.Installer.Domain.Process
{
	public interface IConsoleOutHandler
	{
		void Handle(ConsoleOut consoleOut);
		void Write(ConsoleOut consoleOut);
	}
}