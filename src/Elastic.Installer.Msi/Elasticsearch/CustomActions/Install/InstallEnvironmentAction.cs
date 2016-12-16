using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class InstallEnvironmentAction : CustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(InstallEnvironmentAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallEnvironment;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => Step.InstallFinalize;
		public override When When => When.Before;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult InstallEnvironment(Session session) =>
			session.Handle(() => new SetEnvironmentVariablesTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}
