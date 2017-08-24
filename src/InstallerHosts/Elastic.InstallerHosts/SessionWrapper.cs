using System;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.InstallerHosts
{
	public class SessionWrapper : ISession
	{
		private readonly Session _session;

		public SessionWrapper(Session session)
		{
			this._session = session;
		}

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

		public string GetProductProperty(string property) =>
			_session.GetProductProperty(property);

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

		public bool Uninstalling => this._session.IsUninstalling(); //string.Equals(this.Get<string>("REMOVE"), "All", StringComparison.OrdinalIgnoreCase);

		public bool Upgrading => this._session.IsUpgrading();

		public bool Rollback => this._session.GetMode(InstallRunMode.Rollback);
	}
}