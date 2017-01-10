using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using FluentValidation.Results;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public abstract class InstallationTask
	{
		protected IFileSystem FileSystem { get; }

		protected ISession Session { get; }

		protected ElasticsearchInstallationModel InstallationModel { get; }

		protected string[] Args { get; }

		protected string ActionName => this.GetType().FullName;

		protected InstallationTask(string[] args, ISession session)
			: this(ElasticsearchInstallationModel.Create(new NoopWixStateProvider(), session, args), session, new FileSystem())
		{
			this.Args = args;
		}

		protected InstallationTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
		{
			this.InstallationModel = model;
			this.Session = session;
			this.FileSystem = fileSystem;
			this.Args = new string[] { };
		}

		public bool Execute()
		{
			if (!this.InstallationModel.IsValid)
			{
				var errorPrefix = $"Can not execute {ActionName} the model that it was passed has the following errors";
				var validationFailures = ValidationFailures(this.InstallationModel.ValidationFailures);
				throw new Exception(errorPrefix + Environment.NewLine + validationFailures);
			}
			return this.ExecuteTask();

		}
		protected abstract bool ExecuteTask();

		public string ValidationFailures(IList<ValidationFailure> f) =>
			f.Aggregate(new StringBuilder(), (sb, v) => 
				sb.AppendLine($"{v.PropertyName.ToUpperInvariant().ValidationMessage()}: {v.ErrorMessage}"), sb => sb.ToString());

		protected void ThrowIfModelIsInvalid()
		{
			//TODO implement
		}

		protected bool SamePathAs(string pathA, string pathB)
		{
			if (!string.IsNullOrEmpty(pathA) && !string.IsNullOrEmpty(pathB))
				return 0 == string.Compare(Path.GetFullPath(pathA), Path.GetFullPath(pathB), true);
			else
				return false;
		}
	}
}