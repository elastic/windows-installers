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