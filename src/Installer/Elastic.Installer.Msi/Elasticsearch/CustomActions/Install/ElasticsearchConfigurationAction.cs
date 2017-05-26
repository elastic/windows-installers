﻿using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class ElasticsearchConfigurationAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchConfigurationAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.InstallConfiguration;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => new Step(nameof(ElasticsearchDirectoriesAction));
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchConfiguration(Session session) =>
			session.Handle(() => new EditElasticsearchYamlTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());

	}
}
