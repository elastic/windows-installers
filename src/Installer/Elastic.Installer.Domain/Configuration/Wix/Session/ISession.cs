namespace Elastic.Installer.Domain.Configuration.Wix.Session
{
	public interface ISession
	{
		void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null);
		void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters);
		void Log(string message);
		T Get<T>(string property);
		void Set(string property, string value);
		bool IsUninstalling { get; }
		bool IsInstalling { get; }
		bool IsInstalled { get; }
		bool IsUpgrading { get; }
		bool IsRollback { get; }
		string ProductName { get; }
	}
}
