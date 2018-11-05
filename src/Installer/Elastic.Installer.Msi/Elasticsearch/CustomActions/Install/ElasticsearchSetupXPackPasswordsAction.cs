﻿using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions.Install
{
	public class ElasticsearchSetupXPackPasswordsAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchSetupXPackPasswordsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.SetupXPackPasswords;
		public override Condition Condition => 
			new Condition($"(NOT Installed) AND {nameof(XPackModel.XPackSecurityEnabled).ToUpperInvariant()}~=\"true\" " +
			              $"AND {nameof(XPackModel.XPackLicense).ToUpperInvariant()}~=\"{nameof(XPackLicenseMode.Trial)}\" " +
			              $"AND {nameof(XPackModel.SkipSettingPasswords).ToUpperInvariant()}~=\"false\"");
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => Step.StartServices;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult ElasticsearchSetupXPackPasswords(Session session) =>
			session.Handle(() => new SetupXPackPasswordsTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}