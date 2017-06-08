using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public class NoopPluginStateProvider : IPluginStateProvider
	{
		public List<string> InstalledAfter { get; } = new List<string>();
		public string[] InstalledBefore { get; }

		public NoopPluginStateProvider() { }
		public NoopPluginStateProvider(params string [] installedBefore)
		{
			this.InstalledBefore = installedBefore;
		}

		public void Install(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments) =>
			this.InstalledAfter.Add(plugin);

		public IList<string> InstalledPlugins(string installDirectory, string configDirectory) => 
			this.InstalledBefore?.ToList() ?? new List<string>();

		public void Remove(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments)
		{
		}

		public Task<bool> HasInternetConnection() => Task.FromResult(true);
	}
}