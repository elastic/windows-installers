namespace Elastic.Installer.Domain.Session
{
	public class NoopSession : ISession
	{
		public T Get<T>(string property) => default(T);

		public void Set(string property, string value) { }

		public void Log(string message) { }

		public void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null) { }

		public void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters) { }

		public bool Uninstalling { get; set; }
	}
}