using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Installer.Domain.Configuration.Wix.Session
{
	public class NoopSession : ISession
	{
		public static NoopSession Elasticsearch { get; } = new NoopSession(nameof(Elasticsearch));
		public static NoopSession Kibana { get; } = new NoopSession(nameof(Kibana));
		
		public List<string> LoggedMessages { get; } = new List<string>();
		
		public NoopSession(string productName, Dictionary<string, string> sessionValues = null)
		{
			this.ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
			this.SessionValues = sessionValues ?? new Dictionary<string, string>();
		}

		public T Get<T>(string property) => this.SessionValues.TryGetValue(property, out var value) 
			? (T)Convert.ChangeType(value, typeof(T)) 
			: default(T);

		public void Set(string property, string value) => this.SessionValues[property] = value;

		public void Log(string message) => this.LoggedMessages.Add(message);

		public void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null) { }

		public void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters) { }

		public bool IsUninstalling { get; set; }

		public bool IsInstalling { get; set; }

		public bool IsInstalled { get; set; }

		public bool IsUpgrading { get; set; }

		public bool IsRollback { get; set; }
		
		public string ProductName { get; }
		
		public Dictionary<string, string> SessionValues  { get; }
		
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(NoopSession));
			sb.AppendLine($"- {nameof(IsInstalled)} = {IsInstalled}");
			sb.AppendLine($"- {nameof(IsUninstalling)} = {IsUninstalling}");
			sb.AppendLine($"- {nameof(IsUpgrading)} = {IsUpgrading}");
			sb.AppendLine($"- {nameof(IsRollback)} = {IsRollback}");
			sb.AppendLine($"- {nameof(IsInstalling)} = {IsInstalling}");
			sb.AppendLine($"- {nameof(ProductName)} = {ProductName}");
			sb.AppendLine($"- {nameof(LoggedMessages)}");
			foreach(var l in LoggedMessages)
				sb.AppendLine($"- | {l}");
			return sb.ToString();
		}
	}
}