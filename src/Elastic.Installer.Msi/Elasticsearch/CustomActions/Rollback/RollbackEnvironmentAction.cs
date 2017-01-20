using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	public class RollbackEnvironmentAction : RollbackCustomAction<Elasticsearch>
	{
		public override string Name => nameof(RollbackEnvironmentAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.RollbackEnvironment;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(InstallEnvironmentAction));

		[CustomAction]
		public static ActionResult RollbackEnvironment(Session session) =>
			session.Handle(() => new RemoveEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
