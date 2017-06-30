using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Shared;
using Elastic.InstallerHosts.Tasks;
using Microsoft.Win32;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public abstract class ElasticsearchInstallationTask : InstallationTaskBase
	{
		protected ElasticsearchInstallationModel InstallationModel => this.Model as ElasticsearchInstallationModel;

		protected ElasticsearchInstallationTask(string[] args, ISession session)
			: this(ElasticsearchInstallationModel.Create(
				new WixStateProvider(ProductType.Elasticsearch, session.Version, currentlyInstalling: false), session, args), session, new FileSystem())
		{
			this.Args = args;
		}

		protected ElasticsearchInstallationTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem)
		{ }
	}
}