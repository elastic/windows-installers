using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Domain.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class UninstallPluginsAction : UninstallCustomAction<ElasticsearchProduct>
	{
		public override string Name => nameof(UninstallPluginsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallPlugins;
		public override Step Step => Step.RemoveFiles;
		public override When When => When.Before;

		public override Condition Condition => new Condition("UPGRADINGPRODUCTCODE AND (REMOVE=\"ALL\")");

		[CustomAction]
		public static ActionResult UninstallPlugins(Session session) =>
			session.Handle(() => new RemovePluginsTask(session.ToSetupArguments(), session.ToISession()).Execute());
	}}
