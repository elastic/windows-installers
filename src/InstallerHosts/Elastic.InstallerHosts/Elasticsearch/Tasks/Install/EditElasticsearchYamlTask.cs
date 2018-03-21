using System;
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
			//if xPack step is not relevant assume xpack was already installed and do nothing
			if (xPack.IsRelevant) return;
			
			//only set these if they have no value already
			if (settings.XPackLicenseSelfGeneratedType.IsNullOrEmpty())
				settings.XPackLicenseSelfGeneratedType = Enum.GetName(typeof(XPackLicenseMode), xPack.XPackLicense)?.ToLowerInvariant();
			if (settings.XPackSecurityEnabled == null)
				settings.XPackSecurityEnabled = !xPack.XPackSecurityEnabled ? false : (bool?) null;
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
			var hosts = config.UnicastNodes;
			settings.UnicastHosts = hosts.Any() ? hosts.ToList() : null;
		}
	}
}