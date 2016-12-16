using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class UninstallDirectoriesAction : UninstallCustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(UninstallDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallDirectories;
		public override Step Step => Step.RemoveFiles;
		public override When When => When.Before;
		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");
		

		[CustomAction]
		public static ActionResult UninstallDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}
