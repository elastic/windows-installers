using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Kibana;
using Elastic.InstallerHosts.Tasks;

namespace Elastic.InstallerHosts.Kibana.Tasks
{
	public abstract class KibanaInstallationTask : InstallationTaskBase
	{
		protected KibanaInstallationModel InstallationModel => this.Model as KibanaInstallationModel;

		protected KibanaInstallationTask(string[] args, ISession session)
			: this(KibanaInstallationModel.Create(new WixStateProvider(Product.Kibana, Guid.Parse(session.Get<string>("ProductCode"))), session, args), session, new FileSystem())
		{
			this.Args = args;
		}

		protected KibanaInstallationTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem)
		{ }
	}
}
