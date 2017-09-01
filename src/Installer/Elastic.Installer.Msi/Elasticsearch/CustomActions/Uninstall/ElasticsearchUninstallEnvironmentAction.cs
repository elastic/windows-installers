using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class ElasticsearchUninstallEnvironmentAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchUninstallEnvironmentAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallEnvironment;
		public override Step Step => new Step(nameof(ElasticsearchUninstallServiceAction));
		public override When When => When.After;
		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");

		[CustomAction]
		public static ActionResult ElasticsearchUninstallEnvironment(Session session) =>
			session.Handle(() => new RemoveEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
