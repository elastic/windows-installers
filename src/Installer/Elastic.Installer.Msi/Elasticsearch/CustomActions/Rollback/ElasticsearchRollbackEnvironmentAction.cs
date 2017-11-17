using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	//public class ElasticsearchRollbackEnvironmentAction : RollbackCustomAction<Elasticsearch>
	//{
	//	public override string Name => nameof(ElasticsearchRollbackEnvironmentAction);
	//	public override int Order => (int)ElasticsearchCustomActionOrder.RollbackEnvironment;
	//	public override When When => When.Before;
	//	public override Step Step => new Step(nameof(ElasticsearchEnvironmentAction));

	//	[CustomAction]
	//	public static ActionResult ElasticsearchRollbackEnvironment(Session session) =>
	//		session.Handle(() => new RemoveEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	//}
}
