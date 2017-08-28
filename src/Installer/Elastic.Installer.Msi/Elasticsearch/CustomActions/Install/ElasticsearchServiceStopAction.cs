using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	//public class ElasticsearchServiceStopAction : CustomAction<Elasticsearch>
	//{
	//	public override string Name => nameof(ElasticsearchServiceStopAction);
	//	public override int Order => (int)ElasticsearchCustomActionOrder.InstallStopServiceAction;
	//	// Stop the service when not upgrading
	//	public override Condition Condition => new Condition("NOT UPGRADINGPRODUCTCODE");
	//	public override Return Return => Return.check;
	//	public override Sequence Sequence => Sequence.InstallExecuteSequence;
	//	// Stop the service before the "FilesInUse" dialog can be shown in InstallValidate
	//	public override When When => When.Before;
	//	public override Step Step => Step.InstallValidate;
	//	public override Execute Execute => Execute.immediate;

	//	[CustomAction]
	//	public static ActionResult ElasticsearchServiceStop(Session session) =>
	//		session.Handle(() => new StopServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	//}
}
