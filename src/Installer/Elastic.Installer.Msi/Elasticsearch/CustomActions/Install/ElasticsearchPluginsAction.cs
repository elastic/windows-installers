using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class ElasticsearchPluginsAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchPluginsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallPlugins;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(ElasticsearchJvmOptionsAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchPlugins(Session session) =>
			session.Handle(() => new InstallPluginsTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
