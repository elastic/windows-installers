﻿using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;

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
			if (!xPack.IsRelevant)
			{
				//make sure we unset all xpack related settings because they might prevent a node from starting
				settings.XPackLicenseSelfGeneratedType = null;
				settings.XPackSecurityEnabled = null;
				var xPackKeys = settings.Keys.Where(k => k.StartsWith("xpack.")).ToList();
				foreach (var key in xPackKeys)
					settings.Remove(key);
			}
			else
			{
				settings.XPackLicenseSelfGeneratedType = Enum.GetName(typeof(XPackLicenseMode), xPack.XPackLicense)?.ToLowerInvariant();
				settings.XPackSecurityEnabled = !xPack.XPackSecurityEnabled ? false : (bool?) null;
			}
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