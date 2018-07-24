using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	/// <summary>
	/// Changes the Windows Service installation start type from the default "auto" to "demand".
	/// </summary>
	public class ElasticsearchServiceStartTypeAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchServiceStartTypeAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.ServiceStartType;
		public override Condition Condition => new Condition($"(NOT Installed) " +
			$"AND ({nameof(ServiceModel.InstallAsService).ToUpperInvariant()}~=\"true\" OR {nameof(ServiceModel.InstallAsService).ToUpperInvariant()}=1) " +
			$"AND ({nameof(ServiceModel.StartWhenWindowsStarts).ToUpperInvariant()}~=\"false\" OR {nameof(ServiceModel.StartWhenWindowsStarts).ToUpperInvariant()}=0)");
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => Step.InstallServices;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchServiceStartType(Session session) =>
			session.Handle(() => new SetServiceStartTypeTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}