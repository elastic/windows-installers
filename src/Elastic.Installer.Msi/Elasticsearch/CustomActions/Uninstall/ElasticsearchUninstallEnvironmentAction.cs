using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class ElasticsearchUninstallEnvironmentAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchUninstallEnvironmentAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallEnvironment;
		public override Step Step => Step.InstallExecute;
		public override When When => When.Before;

		[CustomAction]
		public static ActionResult ElasticsearchUninstallEnvironment(Session session) =>
			session.Handle(() => new RemoveEnvironmentVariablesTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
