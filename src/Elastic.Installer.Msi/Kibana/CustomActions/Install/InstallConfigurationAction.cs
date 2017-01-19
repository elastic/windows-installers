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

namespace Elastic.Installer.Msi.Kibana.CustomActions.Install
{
	public class InstallConfigurationAction : CustomAction<Kibana>
	{
		public override string Name => nameof(InstallConfigurationAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallConfiguration;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => Step.InstallFinalize;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult InstallConfiguration(Session session) =>
			session.Handle(() => new EditKibanaYamlTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}
}
