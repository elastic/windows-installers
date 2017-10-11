using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class ElasticsearchSetupXPackPasswordsAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchSetupXPackPasswordsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.SetupXPackPasswords;
		public override Condition Condition => new Condition("(NOT Installed) AND XPACKSECURITYENABLED~=\"true\" AND XPACKLICENSE~=\"Trial\" AND SKIPSETTINGPASSWORDS~=\"false\"");
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(ElasticsearchServiceStartAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchSetupXPackPasswords(Session session) =>
			session.Handle(() => new SetupXPackPasswordsTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}