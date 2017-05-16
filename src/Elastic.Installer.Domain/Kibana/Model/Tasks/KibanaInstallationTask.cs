using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Model.Tasks;
using System.IO.Abstractions;

namespace Elastic.Installer.Domain.Kibana.Model.Tasks
{
	public abstract class KibanaInstallationTask : InstallationTaskBase
	{
		protected KibanaInstallationModel InstallationModel => this.Model as KibanaInstallationModel;

		protected KibanaInstallationTask(string[] args, ISession session)
			: this(KibanaInstallationModel.Create(new NoopWixStateProvider(), session, args), session, new FileSystem())
		{
			this.Args = args;
		}

		protected KibanaInstallationTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem)
		{ }
	}
}
