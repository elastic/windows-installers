using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class ElasticsearchUninstallDirectoriesAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchUninstallDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallDirectories;
		public override Step Step => Step.RemoveFiles;
		public override When When => When.Before;
		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");
		

		[CustomAction]
		public static ActionResult ElasticsearchUninstallDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
