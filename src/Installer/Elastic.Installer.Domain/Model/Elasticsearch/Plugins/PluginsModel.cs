using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Properties;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Plugins
{
	public class PluginsModel : PluginsModelBase<PluginsModel, PluginsModelValidator>
	{
		private static readonly Regex Scheme = new Regex(@".*?:\/\/(.*)");

		public const int HttpPortMinimum = 80;
		public const int HttpsPortMinimum = 443;
		public const int PortMaximum = 65535;
		
		public PluginsModel(IPluginStateProvider pluginStateProvider, IObservable<Tuple<bool, string, string>> pluginDependencies)
			: base(pluginStateProvider)
		{
			EnvironmentVariables = new Dictionary<string, string>
			{
				{ElasticsearchEnvironmentStateProvider.ConfDir, this.ConfigDirectory},
				// might be listing plugins from a Elasticsearch 5.x installation
				{ElasticsearchEnvironmentStateProvider.ConfDirOld, this.ConfigDirectory}
			};

			pluginDependencies.Subscribe(t =>
			{
				this.AlreadyInstalled = t.Item1;
				this.PreviousInstallDirectory = t.Item2;
				this.ConfigDirectory = t.Item3;
				this.Refresh();
			});

			this.WhenAnyValue(vm => vm.InstalledPlugins, l => l.Contains("x-pack"))
				.Subscribe(b => this.PreviousInstallationHasXPack = b);
			
			this.AvailablePlugins.ItemChanged.Select(e => this.Plugins.Any())
				.Subscribe(any =>
				{
					if (!any) this.HasInternetConnection = true;
				});

			Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(async _ => await SetInternetConnection());

			this.SetHttpsProxy = ReactiveCommand.CreateAsyncTask(async _ => await this.HttpsProxyUITask());
			this.WhenAnyObservable(vm => vm.SetHttpsProxy)
				.Subscribe(x =>
				{
					// handle the cancel dialog button. Don't remove the current values
					if (x == null) return;
					
					if (x.Length == 0)
					{
						HttpsProxyHost = null;
						HttpsProxyPort = null;
						return;
					}

					x = Scheme.Replace(x, "$1");
					var proxyParts = x.Split(new [] { ':' }, 2);
					this.HttpsProxyHost = proxyParts[0];

					this.HttpsProxyPort = proxyParts.Length == 2 && int.TryParse(proxyParts[1], out var port) 
						? (int?) port 
						: null;
				});
		}

		public override void Refresh()
		{
			base.Refresh();
			this.HasInternetConnection = true;

			this.HttpProxyHost = null;
			this.HttpProxyPort = null;
			this.HttpsProxyHost = null;
			this.HttpsProxyPort = null;		
		}

		private async Task SetInternetConnection()
		{
			if (!this.Plugins.Any()) return;

			this.HasInternetConnection = await this.PluginStateProvider.HasInternetConnection();
			this.Validate();
		}

		public bool XPackEnabled => true;

		bool? hasInternetConnection;
		public bool? HasInternetConnection
		{
			get => this.hasInternetConnection;
			set => this.RaiseAndSetIfChanged(ref this.hasInternetConnection, value);
		}
		
		bool previousInstallationHasXPack;
		public bool PreviousInstallationHasXPack
		{
			get => this.previousInstallationHasXPack;
			set => this.RaiseAndSetIfChanged(ref this.previousInstallationHasXPack, value);
		}

		private string httpProxy;
		[StaticArgument(nameof(HttpProxyHost))]
		public string HttpProxyHost
		{
			get => this.httpProxy;
			set => this.RaiseAndSetIfChanged(ref this.httpProxy, value);
		}			
		
		private int? httpProxyPort;
		[StaticArgument(nameof(HttpProxyPort))]
		public int? HttpProxyPort
		{
			get => this.httpProxyPort;
			set => this.RaiseAndSetIfChanged(ref this.httpProxyPort, value);
		}		
		
		private string httpsProxyHost;
		[StaticArgument(nameof(HttpsProxyHost))]
		public string HttpsProxyHost
		{
			get => this.httpsProxyHost;
			set => this.RaiseAndSetIfChanged(ref this.httpsProxyHost, value);
		}
		
		private int? httpsProxyPort;
		[StaticArgument(nameof(HttpsProxyPort))]
		public int? HttpsProxyPort
		{
			get => this.httpsProxyPort;
			set => this.RaiseAndSetIfChanged(ref this.httpsProxyPort, value);
		}

		public string HttpsProxyHostAndPort => this.HttpsProxyPort.HasValue
			? $"{this.HttpsProxyHost}:{this.HttpsProxyPort}"
			: this.HttpsProxyHost;

		public Func<Task<string>> HttpsProxyUITask { get; set; }
		public ReactiveCommand<string> SetHttpsProxy { get; }

		private string pluginsStaging;
		[StaticArgument(nameof(PluginsStaging))]
		public string PluginsStaging
		{
			get => this.pluginsStaging;
			set => this.RaiseAndSetIfChanged(ref this.pluginsStaging, value);
		}

		protected override IEnumerable<Plugin> GetPlugins()
		{
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

	}
}
