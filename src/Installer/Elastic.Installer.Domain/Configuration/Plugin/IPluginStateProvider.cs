using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public interface IPluginStateProvider
	{
		void Install(int pluginTicks, string installDirectory, string configDirectory, 
			string plugin, string[] additionalArguments = null, IDictionary<string, string> environmentVariables = null);
		
		void Remove(int pluginTicks, string installDirectory, string configDirectory, 
			string plugin, string[] additionalArguments = null, IDictionary<string, string> environmentVariables = null);
		
		IList<string> InstalledPlugins(string installDirectory, string configDirectory, IDictionary<string, string> environmentVariables = null);

		Task<bool> HasInternetConnection();
	}
}