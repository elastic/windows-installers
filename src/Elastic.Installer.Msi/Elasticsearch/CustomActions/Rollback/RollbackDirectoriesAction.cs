using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	public class RollbackDirectoriesAction : RollbackCustomAction<Elasticsearch>
	{
		public override string Name => nameof(RollbackDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.RollbackDirectories;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(InstallDirectoriesAction));

		[CustomAction]
		public static ActionResult RollbackDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
