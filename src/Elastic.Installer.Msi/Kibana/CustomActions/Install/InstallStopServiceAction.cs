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
	public class KibanaStopServiceAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaStopServiceAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallStopService;
		public override Condition Condition => Condition.Always;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => Step.InstallInitialize;
		public override Execute Execute => Execute.deferred;

		[CustomAction("KibanaStopService")]
		public static ActionResult KibanaStopService(Session session) =>
			session.Handle(() => new StopServiceTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
