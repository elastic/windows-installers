using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	//public class ElasticsearchUninstallServiceAction : UninstallCustomAction<Elasticsearch>
	//{
	//	public override string Name => nameof(ElasticsearchUninstallServiceAction);
	//	public override int Order => (int)ElasticsearchCustomActionOrder.UninstallService;
	//	public override Step Step => new Step(nameof(ElasticsearchUninstallPluginsAction));
	//	public override When When => When.After;

	//	public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");


	//	[CustomAction]
	//	public static ActionResult ElasticsearchUninstallService(Session session) =>
	//		session.Handle(() => new UninstallServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());

	//}
}
