using System.Collections.Generic;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public interface IPluginStateProvider
	{
		void Install(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments);
		void Remove(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments);
		IList<string> InstalledPlugins(string installDirectory, string configDirectory);
	}
}