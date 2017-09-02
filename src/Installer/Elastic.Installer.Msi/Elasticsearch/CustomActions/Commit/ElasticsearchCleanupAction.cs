﻿using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Elasticsearch.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Commit
{
	public class ElasticsearchCleanupAction : CommitCustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchCleanupAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.CleanupInstall;

		[CustomAction]
		public static ActionResult ElasticsearchCleanupInstall(Session session) =>
			session.Handle(() => new CleanupInstallTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}