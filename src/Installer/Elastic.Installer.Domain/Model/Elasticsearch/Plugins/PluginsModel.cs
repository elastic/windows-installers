using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Properties;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Plugins
{
	public class PluginsModel : PluginsModelBase<PluginsModel, PluginsModelValidator>
	{
		private bool _includeSuggest;

		public PluginsModel(IPluginStateProvider pluginStateProvider, IObservable<Tuple<bool, bool, string, string>> pluginDependencies)
			: base(pluginStateProvider)
		{
			pluginDependencies.Subscribe(t =>
			{
				this._includeSuggest = t.Item1;
				this.AlreadyInstalled = t.Item2;
				this.InstallDirectory = t.Item3;
				this.ConfigDirectory = t.Item4;
				this.Refresh();
			});

			this.AvailablePlugins.ItemChanged.Select(e => this.Plugins.Any())
				.Subscribe(any =>
				{
					if (!any) this.HasInternetConnection = true;
				});
			
			Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(async _ => await SetInternetConnection());
		}

		private async Task SetInternetConnection()
		{
			if (!this.Plugins.Any()) return;
			
			this.HasInternetConnection = await this.PluginStateProvider.HasInternetConnection();
			this.Validate();
		}
		
		bool hasInternetConnection;
		public bool HasInternetConnection
		{
			get => this.hasInternetConnection;
			set => this.RaiseAndSetIfChanged(ref this.hasInternetConnection, value);
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

			yield return new Plugin
			{
				PluginType = PluginType.Ingest,
				Url = "ingest-attachment",
				DisplayName = "Ingest Attachment Processor",
				Description = TextResources.PluginsModel_IngestAttachment,
			};

			yield return new Plugin
			{
				PluginType = PluginType.Ingest,
				Url = "ingest-geoip",
				DisplayName = "Ingest GeoIP Processor",
				Description = TextResources.PluginsModel_IngestGeoIP,
			};

			yield return new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-icu",
				DisplayName = "ICU Analysis",
				Description = TextResources.PluginsModel_ICUAnalysis
			};

			yield return new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-kuromoji",
				DisplayName = "Japanese (kuromoji) Analysis",
				Description = TextResources.PluginsModel_JapaneseAnalysis

			};

			yield return new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-phonetic",
				DisplayName = "Phonetic Analysis",
				Description = TextResources.PluginsModel_Phonetic

			};

			yield return new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-smartcn",
				DisplayName = "Smart Chinese Analysis",
				Description = TextResources.PluginsModel_SmartChineseAnalysis
			};

			yield return new Plugin
			{
				PluginType = PluginType.Analysis,
				Url = "analysis-stempel",
				DisplayName = "Stempel Polish Analysis",
				Description = TextResources.PluginsModel_StempelPolishAnalysis

			};

			yield return new Plugin
			{
				PluginType = PluginType.Discovery,
				Url = "discovery-ec2",
				DisplayName = "EC2 Discovery",
				Description = TextResources.PluginsModel_EC2Discovery
			};

			yield return new Plugin
			{
				PluginType = PluginType.Discovery,
				Url = "discovery-azure-classic",
				DisplayName = "Azure Discovery (Classic)",
				Description = TextResources.PluginsModel_AzureDiscovery
			};

			yield return new Plugin
			{
				PluginType = PluginType.Discovery,
				Url = "discovery-gce",
				DisplayName = "GCE Discovery",
				Description = TextResources.PluginsModel_GCEDiscovery
			};

			yield return new Plugin
			{
				PluginType = PluginType.Mapper,
				Url = "mapper-size",
				DisplayName = "Mapper Size",
				Description = TextResources.PluginsModel_MapperSize
			};

			yield return new Plugin
			{
				PluginType = PluginType.Mapper,
				Url = "mapper-murmur3",
				DisplayName = "Mapper Murmur3",
				Description = TextResources.PluginsModel_MapperMurmur3
			};

			yield return new Plugin
			{
				PluginType = PluginType.Scripting,
				Url = "lang-javascript",
				DisplayName = "JavaScript Language",
				Description = TextResources.PluginsModel_JavaScriptLanguagePlugin
			};

			yield return new Plugin
			{
				PluginType = PluginType.Scripting,
				Url = "lang-python",
				DisplayName = "Python Language",
				Description = TextResources.PluginsModel_PythonLanguagePlugin
			};

			yield return new Plugin
			{
				PluginType = PluginType.Snapshot,
				Url = "repository-hdfs",
				DisplayName = "Hadoop HDFS Repository",
				Description = TextResources.PluginsModel_HdfsRepository
			};

			yield return new Plugin
			{
				PluginType = PluginType.Snapshot,
				Url = "repository-s3",
				DisplayName = "S3 Repository",
				Description = TextResources.PluginsModel_S3Repository
			};

			yield return new Plugin
			{
				PluginType = PluginType.Snapshot,
				Url = "repository-azure",
				DisplayName = "Azure Repository",
				Description = TextResources.PluginsModel_AzureRepository
			};

			yield return new Plugin
			{
				PluginType = PluginType.Store,
				Url = "store-smb",
				DisplayName = "Store SMB",
				Description = TextResources.PluginsModel_StoreSmb
			};
		}
		protected override List<string> DefaultPlugins()
		{
			var selectedPlugins = new List<string> { "x-pack" };
			if (!this._includeSuggest) return selectedPlugins;
			selectedPlugins.Add("ingest-attachment");
			selectedPlugins.Add("ingest-geoip");
			return selectedPlugins;
		}
	}
}
