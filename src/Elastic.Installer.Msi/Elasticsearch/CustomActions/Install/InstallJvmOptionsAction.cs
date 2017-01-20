using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class InstallJvmOptionsAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(InstallJvmOptionsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallJvmOptions;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(InstallConfigurationAction));
		public override Execute Execute => Execute.deferred;


		[CustomAction]
		public static ActionResult InstallJvmOptions(Session session) =>
			session.Handle(() => new EditJvmOptionsTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
