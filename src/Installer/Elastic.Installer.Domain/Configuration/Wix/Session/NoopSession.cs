using System;

namespace Elastic.Installer.Domain.Configuration.Wix.Session
{
	public class NoopSession : ISession
	{
		public static NoopSession Elasticsearch { get; } = new NoopSession(nameof(Elasticsearch));
		public static NoopSession Kibana { get; } = new NoopSession(nameof(Kibana));
		
		public NoopSession(string productName)
		{
			this.ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
		}
		
		public T Get<T>(string property) => default(T);

		public void Set(string property, string value) { }

		public void Log(string message) { }

		public void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null) { }

		public void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters) { }

		public bool IsUninstalling { get; set; }

		public bool IsInstalling { get; set; }

		public bool IsInstalled { get; set; }

		public bool IsUpgrading { get; set; }

		public bool IsRollback { get; set; }
		
		public string ProductName { get; }
	}
}