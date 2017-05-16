using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class EditElasticsearchYamlTask : ElasticsearchInstallationTask
	{
		public EditElasticsearchYamlTask(string[] args, ISession session) : base(args, session) { }
		public EditElasticsearchYamlTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 3000;

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Configuring Elasticsearch", "Configuring Elasticsearch: [1]");
			var locations = this.InstallationModel.LocationsModel;
			var config = this.InstallationModel.ConfigurationModel;
			this.Session.SendProgress(1000, "reading elasticsearch.yml from " + locations.ConfigDirectory);
			var yaml = ElasticsearchYamlConfiguration.FromFolder(locations.ConfigDirectory, this.FileSystem);
			this.Session.SendProgress(1000, "updating elasticsearch.yml");
			var settings = yaml.Settings;
			settings.ClusterName = config.ClusterName;
			settings.NodeName = config.NodeName;
			settings.MasterNode = config.MasterNode;
			settings.DataNode = config.DataNode;
			settings.IngestNode = config.IngestNode;
			settings.MemoryLock = config.LockMemory;
			settings.LogsPath = locations.LogsDirectory;
			settings.DataPath = locations.DataDirectory;
			settings.MaxLocalStorageNodes = this.InstallationModel.ServiceModel.InstallAsService ? (int?)1 : null;
			settings.NetworkHost = !string.IsNullOrEmpty(config.NetworkHost) ? config.NetworkHost : null;
			if (config.HttpPort.HasValue)
				settings.HttpPortString = config.HttpPort.Value.ToString(CultureInfo.InvariantCulture);
			if (config.TransportPort.HasValue)
				settings.TransportTcpPortString = config.TransportPort.Value.ToString(CultureInfo.InvariantCulture);
			var hosts = config.UnicastNodes;
			settings.UnicastHosts = hosts.Any() ? hosts.ToList() : null;
			yaml.Save();
			this.Session.SendProgress(1000, "elasticsearch.yml updated");
			return true;
		}
	}
}