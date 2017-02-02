using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Kibana.Model;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Install
{
	public class KibanaEnvironmentAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaEnvironmentAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallEnvironment;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.Before;
		public override Step Step => Step.InstallFinalize;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult KibanaEnvironment(Session session) =>
			session.Handle(() => new SetEnvironmentVariablesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());

	}
}
