using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	//public class ElasticsearchEnvironmentAction : CustomAction<Elasticsearch>
	//{
	//	public override string Name => nameof(ElasticsearchEnvironmentAction);
	//	public override int Order => (int)ElasticsearchCustomActionOrder.InstallEnvironment;
	//	public override Condition Condition => Condition.NOT_Installed;
	//	public override Return Return => Return.check;
	//	public override Sequence Sequence => Sequence.InstallExecuteSequence;
	//	public override Step Step => Step.InstallFiles;
	//	public override When When => When.After;
	//	public override Execute Execute => Execute.deferred;

	//	[CustomAction]
	//	public static ActionResult ElasticsearchEnvironment(Session session) =>
	//		session.Handle(() => new SetEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	//}
}
