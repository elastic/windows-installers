using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Properties;

namespace Elastic.Installer.Domain.Model.Kibana.Plugins
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
	}
}
