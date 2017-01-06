using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Properties;
using ReactiveUI;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Shared.Model.Plugins
{
	public class PluginsModel : StepBase<PluginsModel, PluginsModelValidator>
	{
		public IPluginStateProvider PluginStateProvider { get; }
		private bool _includeSuggest;
		private bool _alreadyInstalled;
		private string _installDirectory;
		private string _configDirectory;

		private ReactiveList<Plugin> _plugins = new ReactiveList<Plugin> { ChangeTrackingEnabled = true };

		public ReactiveList<Plugin> AvailablePlugins
		{
			get { return _plugins; }
			set { this.RaiseAndSetIfChanged(ref _plugins, value); }
		}

		[StaticArgument(nameof(Plugins))]
		public IEnumerable<string> Plugins
		{
			get { return _plugins.Where(p => p.Selected).Select(p => p.Url); }
			set {
				foreach (var p in AvailablePlugins) p.Selected = false;
				if (value == null) return;
				var plugins = AvailablePlugins.Where(p => value.Contains(p.Url)).ToList();
				foreach (var p in plugins) p.Selected = true;
			}
		}

		public PluginsModel(IPluginStateProvider pluginStateProvider, IObservable<Tuple<bool, bool, string, string>> pluginDependencies)
			: this(pluginStateProvider)
		{
			pluginDependencies.Subscribe((t) =>
			{
				this._includeSuggest = t.Item1;
				this._alreadyInstalled = t.Item2;
				this._installDirectory = t.Item3;
				this._configDirectory = t.Item4;
				this.Refresh();
			});
			//this.Refresh();
		}
		public PluginsModel(IPluginStateProvider pluginStateProvider)
		{
			this.Header = "Plugins";
			this.PluginStateProvider = pluginStateProvider;
		}

		public sealed override void Refresh()
		{
			this.AvailablePlugins.Clear();
			AddPlugins();
			var selectedPlugins = new List<string>();
			if (!this._alreadyInstalled)
			{
				selectedPlugins.Add("x-pack");
				if (this._includeSuggest)
				{
					selectedPlugins.Add("ingest-attachment");
					selectedPlugins.Add("ingest-geoip");
				}
			}
			else selectedPlugins = this.PluginStateProvider.InstalledPlugins(this._installDirectory, this._configDirectory).ToList();

			foreach (var plugin in this.AvailablePlugins.Where(p=>selectedPlugins.Contains(p.Url)))
				plugin.Selected = true;
		}

		private void AddPlugins()
		{
			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.XPack,
				Url = "x-pack",
				DisplayName = "X-Pack for the Elastic Stack",
				Description = TextResources.PluginsModel_XPack,
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Ingest,
				Url = "ingest-attachment",
				DisplayName = "Ingest Attachment Processor",
				Description = TextResources.PluginsModel_IngestAttachment,
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Ingest,
				Url = "ingest-geoip",
				DisplayName = "Ingest GeoIP Processor",
				Description = TextResources.PluginsModel_IngestGeoIP,
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-icu",
				DisplayName = "ICU Analysis",
				Description = TextResources.PluginsModel_ICUAnalysis
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-kuromoji",
				DisplayName = "Japanese (kuromoji) Analysis",
				Description = TextResources.PluginsModel_JapaneseAnalysis

			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-phonetic",
				DisplayName = "Phonetic Analysis",
				Description = TextResources.PluginsModel_Phonetic

			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-smartcn",
				DisplayName = "Smart Chinese Analysis",
				Description = TextResources.PluginsModel_SmartChineseAnalysis
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-stempel",
				DisplayName = "Stempel Polish Analysis",
				Description = TextResources.PluginsModel_StempelPolishAnalysis

			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Discovery,
				Url = "discovery-ec2",
				DisplayName = "EC2 Discovery",
				Description = TextResources.PluginsModel_EC2Discovery
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Discovery,
				Url = "discovery-azure",
				DisplayName = "Azure Discovery",
				Description = TextResources.PluginsModel_AzureDiscovery
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Discovery,
				Url = "discovery-gce",
				DisplayName = "GCE Discovery",
				Description = TextResources.PluginsModel_GCEDiscovery
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Mapper,
				Url = "mapper-size",
				DisplayName = "Mapper Size",
				Description = TextResources.PluginsModel_MapperSize
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Mapper,
				Url = "mapper-murmur3",
				DisplayName = "Mapper Murmur3",
				Description = TextResources.PluginsModel_MapperMurmur3
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Scripting,
				Url = "lang-javascript",
				DisplayName = "JavaScript Language",
				Description = TextResources.PluginsModel_JavaScriptLanguagePlugin
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Scripting,
				Url = "lang-python",
				DisplayName = "Python Language",
				Description = TextResources.PluginsModel_PythonLanguagePlugin
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Snapshot,
				Url = "repository-hdfs",
				DisplayName = "Hadoop HDFS Repository",
				Description = TextResources.PluginsModel_HdfsRepository
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Snapshot,
				Url = "repository-s3",
				DisplayName = "S3 Repository",
				Description = TextResources.PluginsModel_S3Repository
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Snapshot,
				Url = "repository-azure",
				DisplayName = "Azure Repository",
				Description = TextResources.PluginsModel_AzureRepository
			});

			this.AvailablePlugins.Add(new Plugin
			{
				PluginType = PluginType.Store,
				Url = "store-smb",
				DisplayName = "Store SMB",
				Description = TextResources.PluginsModel_StoreSmb
			});
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(PluginsModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(Plugins)} = " + string.Join(", ", Plugins));
			return sb.ToString();
		}
	}
}