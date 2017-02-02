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
	public class KibanaPluginsAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaPluginsAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallPlugins;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(KibanaConfigurationAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult KibanaPlugins(Session session) =>
			session.Handle(() => new InstallPluginsTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());

	}
}
