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
using Elastic.Installer.Domain.Elasticsearch.Model;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Uninstall
{
	public class ElasticsearchUninstallPluginsAction : UninstallCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchUninstallPluginsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.UninstallPlugins;
		public override Step Step => Step.RemoveFiles;
		public override When When => When.Before;

		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");

		[CustomAction]
		public static ActionResult ElasticsearchUninstallPlugins(Session session) =>
			session.Handle(() => new RemovePluginsTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}}
