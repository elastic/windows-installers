using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall;
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
	//	public override Condition Condition => new Condition("(NOT Installed) OR (REMOVE=\"ALL\")");
	//	public override Return Return => Return.check;
	//	public override Sequence Sequence => Sequence.InstallExecuteSequence;
	//	public override When When => When.Before;
	//	public override Step Step => Step.RemoveFiles;
	//	public override Execute Execute => Execute.deferred;

	//	[CustomAction]
	//	public static ActionResult ElasticsearchServiceStop(Session session) =>
	//		session.Handle(() => new StopServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	//}
}