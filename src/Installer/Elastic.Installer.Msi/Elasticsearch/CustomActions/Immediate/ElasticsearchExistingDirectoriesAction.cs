using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Immediate
{
	public class ElasticsearchExistingDirectoriesAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchExistingDirectoriesAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.BootstrapPasswordProperty;
		public override Condition Condition => Condition.Always;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => new Step(nameof(ElasticsearchValidateArgumentsAction));
		public override When When => When.Before;
		public override Execute Execute => Execute.immediate;

		[CustomAction]
		public static ActionResult ElasticsearchExistingDirectories(Session session) =>
			session.Handle(() => new ExistingDirectoriesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}