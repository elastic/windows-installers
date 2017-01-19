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
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Shared.Model.Tasks;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public abstract class ElasticsearchInstallationTask : InstallationTaskBase
	{
		protected ElasticsearchInstallationModel InstallationModel { get { return this.Model as ElasticsearchInstallationModel; } }

		protected ElasticsearchInstallationTask(string[] args, ISession session)
			: this(ElasticsearchInstallationModel.Create(new NoopWixStateProvider(), session, args), session, new FileSystem())
		{
			this.Args = args;
		}

		protected ElasticsearchInstallationTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem)
		{ }
	}
}