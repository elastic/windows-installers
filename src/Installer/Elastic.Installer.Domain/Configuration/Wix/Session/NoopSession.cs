namespace Elastic.Installer.Domain.Configuration.Wix.Session
{
	public class NoopSession : ISession
	{
		public T Get<T>(string property) => default(T);

		public string GetProductProperty(string property) => null;

		public void Set(string property, string value) { }

		public void Log(string message) { }

		public void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null) { }

		public void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters) { }

		public bool IsUninstalling { get; set; }

		public bool IsInstalling { get; set; }

		public bool IsInstalled { get; set; }

		public bool IsUpgrading { get; set; }

		public bool IsRollback { get; set; }
	}
}