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

		public SessionWrapper(Session session) => this._session = session;

		public T Get<T>(string property)
		{
			string value;
			if (this._session.TryGetValue(property, out value))
				return (T)Convert.ChangeType(value, typeof(T));
			return default(T);
		}

		public void Set(string property, string value) => this._session.Set(property, value);

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

		/// <summary>
		/// Determines whether MSI is running in "uninstalling" mode.
		/// <para>
		/// This method will fail to retrieve the correct value if called from the deferred custom action and the session properties
		/// that it depends on are not preserved with 'UsesProperties' or 'DefaultUsesProperties'.
		/// </para>
		/// </summary>
		public bool IsUninstalling => this._session.IsUninstalling();

		/// <summary>
		/// Gets a value indicating whether the product is being installed.
		/// <para>
		/// This method will fail to retrieve the correct value if called from the deferred custom action and the session properties
		/// that it depends on are not preserved with 'UsesProperties' or 'DefaultUsesProperties'.
		/// </para>
		/// </summary>
		/// <value>
		/// <c>true</c> if installing; otherwise, <c>false</c>.
		/// </value>
		public bool IsInstalling => this._session.IsInstalling();

		/// <summary>
		/// Determines whether the product associated with the session is installed.
		/// <para>
		/// This method will fail to retrieve the correct value if called from the deferred custom action and the session properties
		/// that it depends on are not preserved with 'UsesProperties' or 'DefaultUsesProperties'.
		/// </para>
		/// </summary>
		public bool IsInstalled => this._session.IsInstalled();

		/// <summary>
		/// Gets a value indicating whether the product is being upgraded.
		/// <para>
		/// This method will fail to retrieve the correct value if called from the deferred custom action and the session properties
		/// that it depends on are not preserved with 'UsesProperties' or 'DefaultUsesProperties'.
		/// </para>
		/// <para>
		/// This method relies on "UPGRADINGPRODUCTCODE" property, which is not set by MSI until previous version is uninstalled. Thus it may not be the
		/// most practical way of detecting upgrades. Use AppSearch.GetProductVersionFromUpgradeCode as a more reliable alternative.
		/// </para>
		/// </summary>
		public bool IsUpgrading => this._session.IsUpgrading();

		public bool IsRollback => this._session.GetMode(InstallRunMode.Rollback);
	}
}