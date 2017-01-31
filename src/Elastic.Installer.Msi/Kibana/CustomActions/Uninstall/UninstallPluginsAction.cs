using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Domain.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Kibana.Model;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Uninstall
{
	public class KibanaUninstallPluginsAction : UninstallCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaUninstallPluginsAction);
		public override int Order => (int)KibanaCustomActionOrder.UninstallPlugins;
		public override Step Step => Step.RemoveFiles;
		public override When When => When.Before;

		public override Condition Condition => new Condition("UPGRADINGPRODUCTCODE AND (REMOVE=\"ALL\")");

		[CustomAction("KibanaUninstallPlugins")]
		public static ActionResult KibanaUninstallPlugins(Session session) =>
			session.Handle(() => new RemovePluginsTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}}
