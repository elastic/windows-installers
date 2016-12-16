using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class InstallServiceAction : CustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(InstallServiceAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallService;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(InstallPluginsAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult InstallService(Session session) =>
			session.Handle(() => new InstallServiceTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}


