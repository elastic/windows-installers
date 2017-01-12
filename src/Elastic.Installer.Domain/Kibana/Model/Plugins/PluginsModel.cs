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
		public PluginsModel(IPluginStateProvider pluginStateProvider) : base(pluginStateProvider) { }

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
	}
}
