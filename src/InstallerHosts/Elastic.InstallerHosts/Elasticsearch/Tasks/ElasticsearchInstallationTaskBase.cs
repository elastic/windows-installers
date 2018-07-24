﻿using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Tasks;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public abstract class ElasticsearchInstallationTaskBase : InstallationTaskBase
	{
		protected ElasticsearchInstallationModel InstallationModel => this.Model as ElasticsearchInstallationModel;

		protected ElasticsearchInstallationTaskBase(string[] args, ISession session, bool installationInProgress = true)
			: this(ElasticsearchInstallationModel.Create(
				new WixStateProvider(Product.Elasticsearch, Guid.Parse(session.Get<string>("ProductCode")), installationInProgress), session, args), session, new FileSystem()
			)
		{
			this.Args = args;
		}

		protected ElasticsearchInstallationTaskBase(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem)
		{ }

		protected string TempProductInstallationDirectory => this.InstallationModel.TempDirectoryConfiguration.TempProductInstallationDirectory;
	}
}