using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class UninstallServiceAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(UninstallServiceAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallService;
		public override Step Step => Step.RemoveFiles;
		public override When When => When.Before;

		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");


		[CustomAction]
		public static ActionResult UninstallService(Session session) =>
			session.Handle(() => new UninstallServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());

	}
}
