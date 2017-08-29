using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class ElasticsearchServiceStartAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchServiceStartAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallStartService;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(ElasticsearchServiceInstallAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchServiceStart(Session session) =>
			session.Handle(() => new StartServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
