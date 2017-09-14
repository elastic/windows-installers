using System;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Immediate
{
	public class ValidateArgumentsTask : ElasticsearchInstallationTaskBase
	{
		public ValidateArgumentsTask(string[] args, ISession session) : base(args, session) { }

		protected override bool ExecuteTask()
		{
			this.Session.Log($"Existing Version Installed: {this.InstallationModel.NoticeModel.ExistingVersionInstalled}");
			this.Session.Log($"Current Version: {this.InstallationModel.NoticeModel.CurrentVersion}");
			this.Session.Log($"Existing Version: {this.InstallationModel.NoticeModel.ExistingVersion}");
			this.Session.Log($"Session Installing: {this.Session.IsInstalling}");
			this.Session.Log($"Session Uninstalling: {this.Session.IsUninstalling}");
			this.Session.Log($"Session Rollback: {this.Session.IsRollback}");
			this.Session.Log($"Session Upgrading: {this.Session.IsUpgrading}");
			this.Session.Log("Passed Args:\r\n" + string.Join(", ", this.SanitizedArgs));
			this.Session.Log("ViewModelState:\r\n" + this.InstallationModel);
			if (!this.InstallationModel.IsValid || this.InstallationModel.Steps.Any(s => !s.IsValid))
			{
				var errorPrefix = $"Cannot continue installation because of the following errors";
				var failures = this.InstallationModel.ValidationFailures
					.Concat(this.InstallationModel.Steps.SelectMany(s => s.ValidationFailures))
					.ToList();

				var validationFailures = ValidationFailures(failures);
				throw new Exception(errorPrefix + Environment.NewLine + validationFailures);
			}
			return true;
		}
	}
}