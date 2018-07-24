using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Commit;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Commit
{
	public class ElasticsearchCleanupAction : CommitCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchCleanupAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.CleanupInstall;

		[CustomAction]
		public static ActionResult ElasticsearchCleanup(Session session) =>
			session.Handle(() => new CleanupInstallTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}