using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	public class RollbackServiceAction : RollbackCustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(RollbackServiceAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.RollbackService;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(InstallServiceAction));

		[CustomAction]
		public static ActionResult RollbackService(Session session) =>
			session.Handle(() => new UninstallServiceTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}
