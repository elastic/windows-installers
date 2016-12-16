using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class InstallStartServiceAction : CustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(InstallStartServiceAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallStartService;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(InstallServiceAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult InstallStartService(Session session) =>
			session.Handle(() => new StartServiceTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}
