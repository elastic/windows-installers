using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Installer.Domain.Extensions;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.Installer.Domain.Session
{
	public static class SessionExtensions
	{
		public static ISession ToISession(this Microsoft.Deployment.WindowsInstaller.Session session) => new SessionWrapper(session);

		private static volatile string[] _cachedSetupArguments;
		private static readonly object Lock = new object();

		public static string[] ToSetupArguments(this Microsoft.Deployment.WindowsInstaller.Session session, IEnumerable<string> allArguments)
		{
			if (session == null) return new string[] { };
			if (_cachedSetupArguments != null) return _cachedSetupArguments;
			lock (Lock)
			{
				if (_cachedSetupArguments != null) return _cachedSetupArguments;

				var arguments = new List<string>();
				foreach (var p in allArguments)
				{
					string v;
					if (session.TryGetValue(p, out v))
						arguments.Add($"{p}={v}");
				}
				_cachedSetupArguments = arguments.ToArray();
			}

			return _cachedSetupArguments;
		}

		public static bool TryGetValue(this Microsoft.Deployment.WindowsInstaller.Session session, string property, out string value)
		{
			value = null;
			try
			{
				if (session.IsActive())
					value = session[property];
				else
					value = session.CustomActionData.ContainsKey(property) 
						? session.CustomActionData[property] 
						: string.Empty;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static void Set(this Microsoft.Deployment.WindowsInstaller.Session session, string property, string value)
		{
			if (session.IsActive())
				session[property] = value;
			else
				session.CustomActionData[property] = value;
		}

		public static bool IsActive(this Microsoft.Deployment.WindowsInstaller.Session session)
		{
			try
			{
				var components = session.Components;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static ActionResult Handle(this Microsoft.Deployment.WindowsInstaller.Session session, Func<bool> action)
		{
			try
			{
				var executed = action();
				return executed ? ActionResult.Success : ActionResult.NotExecuted; 
			}
			catch (Exception e)
			{
				session.Log(e.ToString());
				e.ToEventLog("ElasticsearchMsiInstaller");
				return ActionResult.Failure;
			}
		}

		/// <summary>
		/// Send an action start message. This only works with deferred custom actions
		/// </summary>
		public static MessageResult SendActionStart(this Microsoft.Deployment.WindowsInstaller.Session session, int totalTicks, string actionName, string message, string actionDataTemplate = null)
		{
			// http://www.indigorose.com/webhelp/msifact/Program_Reference/LuaScript/Actions/MSI.ProcessMessage.htm
			// [1] action name (must match the name in the MSI Tables), 
			// [2] description, 
			// [3] template for InstallMessage.ActionData messages e.g. [1], [2], 
			//     etc. relate to the index of values sent in a proceeding ActionData
			using (var record = new Record(actionName, message, actionDataTemplate ?? message))
			{
				session.Message(InstallMessage.ActionStart, record);
			}

			// http://windows-installer-xml-wix-toolset.687559.n2.nabble.com/Update-progress-bar-from-deferred-custom-action-td4994990.html
			// reset the progress bar. 
			// [1] 0 = Reset progress bar, 
			// [2] N = Total ticks in bar, 
			// [3] 0 = Forward progress, 
			// [4] 0 = Execution in progress i.e. Time remaining
			using (var record = new Record(0, totalTicks, 0, 0))
			{
				session.Message(InstallMessage.Progress, record);
			}

			// tell the installer to use Explicit Progress messages
			using (var record = new Record(1, 1, 0))
			{
				return session.Message(InstallMessage.Progress, record);
			}
		}

		/// <summary>
		/// Send a progress message. This only works with deferred custom actions
		/// </summary>
		public static MessageResult SendProgress(this Microsoft.Deployment.WindowsInstaller.Session session, int tickIncrement, params object[] actionDataTemplateParameters)
		{
			if (actionDataTemplateParameters.Any())
			{
				// [N] = These are values for the placeholders in the ActionStart template that precedes the ActionData.
				using (var record = new Record(actionDataTemplateParameters))
				{
					session.Message(InstallMessage.ActionData, record);
				}
			}

			// [1] 2 = Increment the progress bar, 
			// [2] N = Number of ticks to move the progress bar, 
			// [3] 0 = Unused (but exists in sample)
			using (var record = new Record(2, tickIncrement, 0))
			{
				return session.Message(InstallMessage.Progress, record);
			}
		}
	}
}
