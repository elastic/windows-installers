using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Model.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Model.Plugins
{
	public class PluginsModel : PluginsModelBase<PluginsModel, PluginsModelValidator>
	{
		private bool _alreadyInstalled;

		public PluginsModel(IPluginStateProvider pluginStateProvider, IObservable<Tuple<bool, string, string>> pluginDependencies) : base(pluginStateProvider)
		{
			pluginDependencies.Subscribe(t =>
			{
				this._alreadyInstalled = t.Item1;
				this.InstallDirectory = t.Item2;
				this.ConfigDirectory = t.Item3;
			});
		}

		protected override IEnumerable<Plugin> GetPlugins()
		{
			yield return new Plugin
			{
				PluginType = PluginType.XPack,
				Url = "x-pack",
				DisplayName = "X-Pack",
				Description = TextResources.PluginsModel_XPack,
			};
		}

		protected override List<string> DefaultPlugins() => new List<string> { "x-pack" };
	}
}
