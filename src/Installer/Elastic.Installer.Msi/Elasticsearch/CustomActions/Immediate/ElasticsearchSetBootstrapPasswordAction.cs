using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Immediate
{
	public class ElasticsearchSetBootstrapPasswordAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchSetBootstrapPasswordAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.BootstrapPasswordProperty;
		public override Condition Condition => 
			new Condition($"(NOT Installed) AND {nameof(XPackModel.BootstrapPassword).ToUpperInvariant()}=\"\"");
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => new Step(nameof(ElasticsearchValidateArgumentsAction));
		public override When When => When.After;
		public override Execute Execute => Execute.immediate;

		[CustomAction]
		public static ActionResult ElasticsearchSetBootstrapPassword(Session session) =>
			session.Handle(() => new SetBootstrapPasswordPropertyTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}