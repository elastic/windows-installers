using System.IO.Abstractions;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class EditJvmOptionsTask : ElasticsearchInstallationTaskBase
	{
		public EditJvmOptionsTask(string[] args, ISession session) : base(args, session) { }
		public EditJvmOptionsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(1000, ActionName, "Updating Elasticsearch JVM options", "Elasticsearch JVM options: [1]");
			var selectedMemory = this.InstallationModel.ConfigurationModel.SelectedMemory;
			var heapSize = $"{selectedMemory}m";
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var options = LocalJvmOptionsConfiguration.FromFolder(configDirectory, this.FileSystem);
			options.Xmx = heapSize;
			options.Xms = heapSize;
			options.Save();
			this.Session.SendProgress(1000, "updated heap size to " + selectedMemory + "m");
			return true;
		}
	}
}