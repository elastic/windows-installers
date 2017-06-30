using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.InstallerHosts
{
	public class SessionWrapper : ISession
	{
		private readonly Session _session;

		public SessionWrapper(Session session)
		{
			this._session = session;
		}

		public string Version => 
			!this._session.TryGetValue("CurrentVersion", out string version) ? null : version;

		public T Get<T>(string property)
		{
			string value;
			if (this._session.TryGetValue(property, out value))
				return (T)Convert.ChangeType(value, typeof(T));
			return default(T);
		}

		public void Set(string property, string value)
		{
			this._session.Set(property, value);
		}

		public void Log(string message) => this._session.Log(message);

		public void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null)
		{
			this._session.SendActionStart(totalTicks, actionName, message, actionDataTemplate);
			Log($"{actionName}: {message}");
		}

		public void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters)
		{
			this._session.SendProgress(tickIncrement, actionDataTemplateParameters);
			if (actionDataTemplateParameters.Any())
				Log($"{string.Join(" ", actionDataTemplateParameters.Select((v, i) => $"[{i}] {v}"))}");
		}

		public bool Uninstalling => string.Equals(this.Get<string>("REMOVE"), "All", StringComparison.OrdinalIgnoreCase);
	}
}