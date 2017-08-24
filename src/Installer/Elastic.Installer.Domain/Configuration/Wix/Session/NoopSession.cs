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

		public bool Uninstalling { get; set; }

		public bool Upgrading { get; set; }

		public bool Rollback { get; set; }
	}
}