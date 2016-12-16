using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	//TODO unused??
	public class InstallStopServiceAction : CustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(InstallStopServiceAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallStopServiceAction;
		public override Condition Condition => Condition.Always;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => Step.InstallInitialize;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult InstallStopService(Session session) =>
			session.Handle(() => new StopServiceTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}
