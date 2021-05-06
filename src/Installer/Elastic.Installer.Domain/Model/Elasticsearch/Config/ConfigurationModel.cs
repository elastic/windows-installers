using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Model.Base;
using Microsoft.VisualBasic.Devices;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Config
{
	public class ConfigurationModel : StepBase<ConfigurationModel, ConfigurationModelValidator>
	{
		private readonly LocalJvmOptionsConfiguration _localJvmOptions;
		private readonly ElasticsearchYamlSettings _yamlSettings;

		public const string DefaultClusterName = "elasticsearch";
		public static readonly string DefaultNodeName = Environment.MachineName;
		public const bool DefaultMasterNode = true;
		public const bool DefaultDataNode = true;
		public const bool DefaultIngestNode = true;
		public const bool DefaultMemoryLock = false;
		public static ulong DefaultTotalPhysicalMemory { get; } = GetTotalPhysicalMemory();
		public const ulong DefaultHeapSizeThreshold = 4096;
		public const ulong DefaultDistributionHeapSize = 2048;

		public static ulong DefaultHeapSize => 
			DefaultTotalPhysicalMemory <= DefaultHeapSizeThreshold
				? Math.Min(DefaultTotalPhysicalMemory / 2, CompressedOrdinaryPointersThreshold)
				: DefaultDistributionHeapSize;

		public const ulong CompressedOrdinaryPointersThreshold = 30500;
		public const int HttpPortMinimum = 80;
		public const int TransportPortMinimum = 1024;
		public const int PortMaximum = 65535;
		public const int HttpPortDefault = 9200;
		public const int TransportPortDefault = 9300;
		
		public ConfigurationModel(ElasticsearchYamlConfiguration yamlConfiguration,
			LocalJvmOptionsConfiguration localJvmOptions, IObservable<bool> upgradingFrom6OrNewInstallation)
		{
			this.Header = "Configuration";
			this._localJvmOptions = localJvmOptions;
			this._yamlSettings = yamlConfiguration?.Settings;
			upgradingFrom6OrNewInstallation.Subscribe(b => this.UpgradingFrom6OrNewInstallation = b);
			this.Refresh();

			this.AddSeedHost = ReactiveCommand.CreateAsyncTask(async _ => await this.AddSeedHostUserInterfaceTask());
			this.WhenAnyObservable(vm => vm.AddSeedHost)
				.Subscribe(x =>
				{
					if (string.IsNullOrWhiteSpace(x)) return;
					var nodes = x
						.Split(',')
						.Select(node => node.Trim())
						.Where(n => !string.IsNullOrEmpty(n))
						.Distinct();

					foreach (var n in nodes)
						this.SeedHosts.Add(n);
				});

			this.WhenAny(
				vm => vm.TotalPhysicalMemory,
				(maxMemory) => Math.Min(maxMemory.Value / 2, CompressedOrdinaryPointersThreshold)
				)
				.ToProperty(this, vm => vm.MaxSelectedMemory, out maxSelectedMemory);

			var canRemoveNode = this.WhenAny(vm => vm.SelectedSeedHost, (selected) => !string.IsNullOrWhiteSpace(selected.GetValue()));
			this.RemoveSeedHost = ReactiveCommand.Create(canRemoveNode);
			this.RemoveSeedHost.Subscribe(x =>
			{
				this.SeedHosts.Remove(this.SelectedSeedHost);
			});
			this.WhenAnyValue(vm => vm.MasterNode).Subscribe(b =>
			{
				// if we unset master node make sure InitialMaster is not set either.
				if (!b) this.InitialMaster = false;
			});
		}

		public sealed override void Refresh()
		{
			this.ClusterName = this._yamlSettings?.ClusterName ?? DefaultClusterName;
			this.NodeName = this._yamlSettings?.NodeName ?? DefaultNodeName;
			this.MasterNode = this._yamlSettings?.MasterNode ?? DefaultMasterNode;
			this.DataNode = this._yamlSettings?.DataNode ?? DefaultDataNode;
			this.IngestNode = this._yamlSettings?.IngestNode ?? DefaultIngestNode;
			this.SeedHosts = ReadSeedHosts();
			this.LockMemory = this._yamlSettings?.MemoryLock ?? DefaultMemoryLock;
			this.TotalPhysicalMemory = DefaultTotalPhysicalMemory;
			this.SelectedMemory = this._localJvmOptions?.ConfiguredHeapSize ?? DefaultHeapSize;
			this.NetworkHost = this._yamlSettings?.NetworkHost;
			this.HttpPort = this._yamlSettings?.HttpPort ?? HttpPortDefault;
			this.TransportPort = this._yamlSettings?.TransportTcpPort ?? TransportPortDefault;
		}

		private ReactiveList<string> ReadSeedHosts()
		{
			if (this._yamlSettings?.UnicastHosts != null && this._yamlSettings?.SeedHosts == null)
				return new ReactiveList<string>(this._yamlSettings.UnicastHosts);
			return this._yamlSettings?.SeedHosts != null
				? new ReactiveList<string>(this._yamlSettings?.SeedHosts)
				: new ReactiveList<string>();
		}

		private static ulong GetTotalPhysicalMemory()
		{
			var total = new ComputerInfo().TotalPhysicalMemory;
			var totalMb = total / (1024.0 * 1024.0);
			var memory = Convert.ToUInt64(totalMb);
			return memory;
		}

		public Func<Task<string>> AddSeedHostUserInterfaceTask { get; set; }
		public ReactiveCommand<string> AddSeedHost { get; }
		public ReactiveCommand<object> RemoveSeedHost { get; }

		// ReactiveUI conventions do not change
		// ReSharper disable InconsistentNaming
		// ReSharper disable ArrangeTypeMemberModifiers
		private ReactiveList<string> seedHosts = new ReactiveList<string>();
		[Argument(nameof(SeedHosts))]
		public ReactiveList<string> SeedHosts
		{
			get => seedHosts;
			set { this.RaiseAndSetIfChanged(ref seedHosts, new ReactiveList<string>(value?.Where(n => !string.IsNullOrEmpty(n)).Select(n => n.Trim()).ToList())); }
		}

		string selectedSeedHost;
		public string SelectedSeedHost
		{
			get => this.selectedSeedHost;
			set => this.RaiseAndSetIfChanged(ref this.selectedSeedHost, value);
		}

		string clusterName;
		[StaticArgument(nameof(ClusterName))]
		public string ClusterName
		{
			get => this.clusterName;
			set => this.RaiseAndSetIfChanged(ref this.clusterName, value);
		}

		string nodeName;
		[SetPropertyActionArgument(nameof(NodeName), "[%COMPUTERNAME]")]
		public string NodeName
		{
			get => nodeName;
			set => this.RaiseAndSetIfChanged(ref this.nodeName, value);
		}

		bool masterNode;
		[StaticArgument(nameof(MasterNode))]
		public bool MasterNode
		{
			get => this.masterNode;
			set => this.RaiseAndSetIfChanged(ref this.masterNode, value);
		}

		bool dataNode;
		[StaticArgument(nameof(DataNode))]
		public bool DataNode
		{
			get => this.dataNode;
			set => this.RaiseAndSetIfChanged(ref this.dataNode, value);
		}

		bool ingestNode;
		[StaticArgument(nameof(IngestNode))]
		public bool IngestNode
		{
			get => this.ingestNode;
			set => this.RaiseAndSetIfChanged(ref this.ingestNode, value);
		}

		ulong totalPhysicalMemory;
		public ulong TotalPhysicalMemory
		{
			get => this.totalPhysicalMemory;
			set => this.RaiseAndSetIfChanged(ref this.totalPhysicalMemory, value);
		}

		readonly ObservableAsPropertyHelper<ulong> maxSelectedMemory;
		public ulong MaxSelectedMemory => maxSelectedMemory.Value;
		public ulong MinSelectedMemory => 256;

		ulong selectedMemory;
		[Argument(nameof(SelectedMemory))]
		public ulong SelectedMemory
		{
			get => this.selectedMemory;
			set => this.RaiseAndSetIfChanged(ref this.selectedMemory, value);
		}

		bool lockMemory;
		[StaticArgument(nameof(LockMemory))]
		public bool LockMemory
		{
			get => this.lockMemory;
			set => this.RaiseAndSetIfChanged(ref this.lockMemory, value);
		}

		bool initialMaster;
		[StaticArgument(nameof(InitialMaster))]
		public bool InitialMaster
		{
			get => this.initialMaster;
			set => this.RaiseAndSetIfChanged(ref this.initialMaster, value);
		}

		bool upgradingFrom6OrNewInstallation;
		public bool UpgradingFrom6OrNewInstallation
		{
			get => this.upgradingFrom6OrNewInstallation;
			set => this.RaiseAndSetIfChanged(ref this.upgradingFrom6OrNewInstallation, value);
		}

		string networkHost;
		[Argument(nameof(NetworkHost))]
		public string NetworkHost
		{
			get => this.networkHost;
			set => this.RaiseAndSetIfChanged(ref this.networkHost, value);
		}

		int? httpPort;
		[StaticArgument(nameof(HttpPort))]
		public int? HttpPort
		{
			get => this.httpPort;
			set => this.RaiseAndSetIfChanged(ref this.httpPort, value);
		}

		int? transportPort;
		[StaticArgument(nameof(TransportPort))]
		public int? TransportPort
		{
			get => this.transportPort;
			set => this.RaiseAndSetIfChanged(ref this.transportPort, value);
		}
		
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(ConfigurationModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(SeedHosts)} = " + string.Join(", ", SeedHosts));
			sb.AppendLine($"- {nameof(ClusterName)} = " + ClusterName);
			sb.AppendLine($"- {nameof(NodeName)} = " + NodeName);
			sb.AppendLine($"- {nameof(MasterNode)} = " + MasterNode);
			sb.AppendLine($"- {nameof(DataNode)} = " + DataNode);
			sb.AppendLine($"- {nameof(IngestNode)} = " + IngestNode);
			sb.AppendLine($"- {nameof(TotalPhysicalMemory)} = " + TotalPhysicalMemory.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine($"- {nameof(SelectedMemory)} = " + SelectedMemory.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine($"- {nameof(InitialMaster)} = " + InitialMaster);
			sb.AppendLine($"- {nameof(LockMemory)} = " + LockMemory);
			sb.AppendLine($"- {nameof(NetworkHost)} = " + NetworkHost);
			sb.AppendLine($"- {nameof(HttpPort)} = " + HttpPort);
			sb.AppendLine($"- {nameof(TransportPort)} = " + TransportPort);
			return sb.ToString();
		}

	}
}