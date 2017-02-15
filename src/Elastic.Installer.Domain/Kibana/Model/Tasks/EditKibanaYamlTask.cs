using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Kibana.Configuration.FileBased;
using Elastic.Installer.Domain.Session;


namespace Elastic.Installer.Domain.Kibana.Model.Tasks
{
	public class EditKibanaYamlTask : KibanaInstallationTask
	{
		public EditKibanaYamlTask(string[] args, ISession session) : base(args, session) { }
		public EditKibanaYamlTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 3000;

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Configuring Kibana", "Configuring Kibana: [1]");
			var locations = this.InstallationModel.LocationsModel;
			var config = this.InstallationModel.ConfigurationModel;
			var connecting = this.InstallationModel.ConnectingModel;
			this.Session.SendProgress(1000, "reading kibana.yml from " + locations.ConfigDirectory);
			var yaml = KibanaYamlConfiguration.FromFolder(locations.ConfigDirectory, this.FileSystem);
			this.Session.SendProgress(1000, "updating kibana.yml");
			var settings = yaml.Settings;
			settings.LoggingDestination = locations.LogsFile;
			settings.ServerHost = config.HostName;
			settings.ServerPort = config.HttpPort;
			settings.ServerBasePath = config.BasePath;
			settings.ServerName = config.ServerName;
			settings.ServerKey = config.ServerKey;
			settings.ServerCert = config.ServerCertificate;
			settings.ServerDefaultRoute = config.DefaultRoute;
			settings.ElasticsearchUrl = connecting.Url;
			settings.KibanaIndex = connecting.IndexName;
			settings.ElasticsearchKey = connecting.ElasticsearchKey;
			settings.ElasticsearchCert = connecting.ElasticsearchCert;
			settings.ElasticsearchCA = connecting.ElasticsearchCA;
			yaml.Save();

			// log file needs to exist for plugin installation
			if (locations.LogsFile != "stdout" && !this.FileSystem.File.Exists(locations.LogsFile))
			{
				var fileInfo = this.FileSystem.FileInfo.FromFileName(locations.LogsFile);
				fileInfo.Directory.Create();
				using (this.FileSystem.File.Create(locations.LogsFile)) { }
			}

			this.Session.SendProgress(1000, "kibana.yml updated");
			return true;
		}
	}
}
