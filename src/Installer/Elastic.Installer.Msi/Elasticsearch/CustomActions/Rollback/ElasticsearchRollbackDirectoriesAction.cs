using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Rollback;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	public class ElasticsearchRollbackDirectoriesAction : RollbackCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchRollbackDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.RollbackDirectories;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(ElasticsearchDirectoriesAction));

		[CustomAction]
		public static ActionResult ElasticsearchRollbackDirectories(Session session) =>
			session.Handle(() => new RollbackDirectoriesTask(
				session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
