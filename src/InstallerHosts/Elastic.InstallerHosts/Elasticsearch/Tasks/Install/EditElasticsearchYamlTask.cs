using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using WixSharp;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class EditElasticsearchYamlTask : ElasticsearchInstallationTaskBase
	{
		public EditElasticsearchYamlTask(string[] args, ISession session) : base(args, session) { }

		public EditElasticsearchYamlTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 6000;

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Configuring Elasticsearch", "Configuring Elasticsearch: [1]");
			var locations = this.InstallationModel.LocationsModel;
			this.Session.SendProgress(1000, "reading elasticsearch.yml from " + locations.ConfigDirectory);
			var yaml = ElasticsearchYamlConfiguration.FromFolder(locations.ConfigDirectory, this.FileSystem);

			var settings = yaml.Settings;
			this.ApplyConfigurationModel(settings);
			this.ApplyLocationsModel(settings, locations);
			this.ApplyServiceModel(settings);
			this.ApplyXPackModel(settings);
			yaml.Save();
			this.Session.SendProgress(1000, "elasticsearch.yml updated");
			return true;
		}

		private void ApplyXPackModel(ElasticsearchYamlSettings settings)
		{
			this.Session.SendProgress(1000, "updating elasticsearch.yml with values from x-pack model if needed");
			var xPack = this.InstallationModel.XPackModel;
			
			//only set if no value already
			if (settings.XPackLicenseSelfGeneratedType.IsNullOrEmpty())
				settings.XPackLicenseSelfGeneratedType = Enum.GetName(typeof(XPackLicenseMode), xPack.XPackLicense)?.ToLowerInvariant();

			if (settings.XPackSecurityEnabled == null)
				settings.XPackSecurityEnabled = xPack.XPackSecurityEnabled;
		}

		private void ApplyServiceModel(ElasticsearchYamlSettings settings)
		{
			this.Session.SendProgress(1000, "updating elasticsearch.yml with values from locations model");
			settings.MaxLocalStorageNodes = this.InstallationModel.ServiceModel.InstallAsService ? (int?) 1 : null;
		}

		private void ApplyLocationsModel(ElasticsearchYamlSettings settings, LocationsModel locations)
		{
			this.Session.SendProgress(1000, "updating elasticsearch.yml with values from locations model");
			settings.LogsPath = locations.LogsDirectory;
			settings.DataPath = locations.DataDirectory;
		}

		private void ApplyConfigurationModel(ElasticsearchYamlSettings settings)
		{
			this.Session.SendProgress(1000, "updating elasticsearch.yml with values from configuration model");
			var config = this.InstallationModel.ConfigurationModel;
			var version = this.InstallationModel.NoticeModel.CurrentVersion;
			this.Session.Log($"Persisting configuration to elasticsearch.yml for version: {version}");
			
			settings.ClusterName = config.ClusterName;
			settings.NodeName = config.NodeName;
			settings.MasterNode = config.MasterNode;
			settings.DataNode = config.DataNode;
			settings.IngestNode = config.IngestNode;
			settings.MemoryLock = config.LockMemory;
			settings.NetworkHost = !string.IsNullOrEmpty(config.NetworkHost) ? config.NetworkHost : null;
			if (config.HttpPort.HasValue)
				settings.HttpPortString = config.HttpPort.Value.ToString(CultureInfo.InvariantCulture);
			if (config.TransportPort.HasValue)
				settings.TransportTcpPortString = config.TransportPort.Value.ToString(CultureInfo.InvariantCulture);
			var hosts = config.SeedHosts;

			var hostsList = hosts.Any() ? hosts.ToList() : null;
			if (version.Major >= 7)
			{
				settings.SeedHosts = hostsList;
				var doesNotHaveInitialMasterNodesInYaml = settings.InitialMasterNodes == null || !settings.InitialMasterNodes.Any() ;
				this.Session.Log($"Yaml does not already have cluster.initial_master_nodes: {doesNotHaveInitialMasterNodesInYaml}");
				if (doesNotHaveInitialMasterNodesInYaml && !string.IsNullOrEmpty(config.NodeName) && config.InitialMaster)
				{
					this.Session.Log($"Initial Master flag set defaulting to current node name: {config.NodeName}");
					settings.InitialMasterNodes = new List<string>{ config.NodeName };
					
				}
				this.Session.Log($"Making sure we are not carrying over unicast host or minimum master nodes configuration into >= 7.x of Elasticsearch");
				settings.UnicastHosts = null;
				settings.MinimumMasterNodes = null;
			}
			else settings.UnicastHosts = hostsList;
		}
	}
}