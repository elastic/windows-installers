using System;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class ValidateArgumentsTask : ElasticsearchInstallationTask
	{
		public ValidateArgumentsTask(string[] args, ISession session) : base(args, session) { }

		protected override bool ExecuteTask()
		{
			Session.Log($"AlreadyInstalled: {this.InstallationModel.NoticeModel.AlreadyInstalled}");
			Session.Log($"Current Version: {this.InstallationModel.NoticeModel.CurrentVersion}");
			Session.Log($"Existing Version: {this.InstallationModel.NoticeModel.ExistingVersion}");
			Session.Log($"Session Uninstalling: {this.Session.IsUninstalling}");
			Session.Log($"Session Rollback: {this.Session.IsRollback}");
			Session.Log($"Session Upgrading: {this.Session.IsUpgrading}");


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