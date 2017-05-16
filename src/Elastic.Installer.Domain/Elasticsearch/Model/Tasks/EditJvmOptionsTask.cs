using System.IO.Abstractions;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class EditJvmOptionsTask : ElasticsearchInstallationTask
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