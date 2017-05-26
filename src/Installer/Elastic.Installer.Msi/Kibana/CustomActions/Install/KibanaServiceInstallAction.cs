using Elastic.Installer.Msi.CustomActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using Elastic.Installer.Domain.Model.Kibana;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Install
{
	public class KibanaServiceInstallAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaServiceInstallAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallService;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(KibanaPluginsAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult KibanaServiceInstall(Session session) => 
			session.Handle(() => new InstallServiceTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}


