using Elastic.Installer.Domain.Session;
using System;
using System.Linq;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class ValidateArgumentsTask : InstallationTask
	{
		public ValidateArgumentsTask(string[] args, ISession session) : base(args, session) { }

		protected override bool ExecuteTask()
		{
			this.Session.Log("Passed Args:\r\n" + string.Join(", ", this.Args));
			this.Session.Log("ViewModelState:\r\n" + this.InstallationModel.ToString());
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