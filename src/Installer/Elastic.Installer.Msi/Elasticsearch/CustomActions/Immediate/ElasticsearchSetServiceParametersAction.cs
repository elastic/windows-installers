using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Immediate
{
	public class ElasticsearchSetServiceParametersAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchSetServiceParametersAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.ServiceParameters;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => new Step(nameof(ElasticsearchSetBootstrapPasswordAction));
		public override When When => When.After;
		public override Execute Execute => Execute.immediate;

		[CustomAction]
		public static ActionResult ElasticsearchSetServiceParameters(Session session) =>
			session.Handle(() => new SetServiceParametersTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}