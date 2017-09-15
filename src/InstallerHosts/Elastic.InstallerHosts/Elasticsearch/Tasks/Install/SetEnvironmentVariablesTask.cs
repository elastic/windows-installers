using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetEnvironmentVariablesTask : ElasticsearchInstallationTaskBase
	{
		public SetEnvironmentVariablesTask(string[] args, ISession session) : base(args, session) { }
		public SetEnvironmentVariablesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(1000, ActionName, "Setting environment variables", "[1]");
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;

			var esState = this.InstallationModel.ElasticsearchEnvironmentConfiguration;
			esState.SetEsHomeEnvironmentVariable(installDirectory);
			esState.SetEsConfigEnvironmentVariable(configDirectory);
			this.Session.SendProgress(1000, "Environment variables set");
			return true;
		}
	}
}