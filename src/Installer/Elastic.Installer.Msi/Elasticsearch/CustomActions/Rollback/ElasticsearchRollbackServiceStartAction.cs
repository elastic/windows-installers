using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Rollback
{
	public class ElasticsearchRollbackServiceStartAction : RollbackCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchRollbackServiceStartAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.RollbackServiceStart;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(ElasticsearchServiceStartAction));

		[CustomAction]
		public static ActionResult ElasticsearchRollbackServiceStart(Session session) =>
			session.Handle(() => new RollbackServiceTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
