using Elastic.Installer.Domain.Session;
using System.IO.Abstractions;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class RemoveEnvironmentVariablesTask : InstallationTask
	{
		public RemoveEnvironmentVariablesTask(string[] args, ISession session) : base(args, session) { }
		public RemoveEnvironmentVariablesTask(InstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			if (this.InstallationModel.NoticeModel.AlreadyInstalled && !this.Session.Uninstalling)
				return true;

			this.Session.SendActionStart(1000, ActionName, "Removing environment variables", "[1]");

			var esState = this.InstallationModel.ElasticsearchEnvironmentState;
			esState.SetEsHomeEnvironmentVariable(null);
			esState.SetEsConfigEnvironmentVariable(null);
			this.Session.SendProgress(1000, "Environment variables removed");
			return true;
		}
	}
}