using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	//public class ElasticsearchRollbackInstallServiceAction : RollbackCustomAction<Elasticsearch>
	//{
	//	public override string Name => nameof(ElasticsearchRollbackInstallServiceAction);
	//	public override int Order => (int)ElasticsearchCustomActionOrder.RollbackServiceInstall;
	//	public override When When => When.Before;
	//	public override Step Step => new Step(nameof(ElasticsearchServiceInstallAction));

	//	[CustomAction]
	//	public static ActionResult ElasticsearchRollbackInstallService(Session session) =>
	//		session.Handle(() => new UninstallServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	//}
}
