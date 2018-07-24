using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Uninstall;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class ElasticsearchUninstallDirectoriesAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchUninstallDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallDirectories;
		public override Step Step => Step.RemoveFolders;
		public override When When => When.After;
		public override Condition Condition => Condition.BeingRemoved;
		
		[CustomAction]
		public static ActionResult ElasticsearchUninstallDirectories(Session session) =>
			session.Handle(() => new UninstallDirectoriesTask(
				session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
