using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class ElasticsearchUninstallDirectoriesAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchUninstallDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallDirectories;
		public override Step Step => Step.RemoveEnvironmentStrings; //new Step(nameof(ElasticsearchUninstallEnvironmentAction));
		public override When When => When.After;
		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");
		

		[CustomAction]
		public static ActionResult ElasticsearchUninstallDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(
				session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession(), rollBack: false).Execute());
	}
}
