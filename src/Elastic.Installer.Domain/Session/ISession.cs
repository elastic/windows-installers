using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.Installer.Domain.Session
{
	public interface ISession
	{
		void SendActionStart(int totalTicks, string actionName, string message, string actionDataTemplate = null);
		void SendProgress(int tickIncrement, params object[] actionDataTemplateParameters);
		void Log(string message);
		T Get<T>(string property);
		void Set(string property, string value);
		bool Uninstalling { get; }
	}
}
