using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	/// <summary>
	/// Sets the internal ServiceAccount and ServicePassword properties used for service installation
	/// </summary>
	public class SetServiceParametersTask : ElasticsearchInstallationTaskBase
	{
		public SetServiceParametersTask(string[] args, ISession session) 
			: base(args, session) { }

		public SetServiceParametersTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			var serviceModel = this.InstallationModel.ServiceModel;
			var username = serviceModel.User;
			var password = serviceModel.Password;

			if (serviceModel.UseExistingUser)
			{
				Session.Log($"Setting {ServiceModel.ServiceAccount} to {nameof(serviceModel.User).ToUpperInvariant()} " +
				            $"and {ServiceModel.ServicePassword} to {nameof(serviceModel.Password).ToUpperInvariant()}");

				// if no domain associated, assume environment user domain name
				var userParts = username.Split(new[] { '\\' }, 2);
				if (userParts.Length == 1)
					username = @".\" + username;

				Session.Set(ServiceModel.ServiceAccount, username);
				Session.Set(ServiceModel.ServicePassword, password);
			}
			else if (serviceModel.UseNetworkService)
			{
				Session.Log($@"Setting ServiceAccount to Network Service account");
				Session.Set(ServiceModel.ServiceAccount, @".\NetworkService");
			}
			else if (serviceModel.UseLocalSystem)
			{
				Session.Log($@"Setting ServiceAccount to Local System account");
				Session.Set(ServiceModel.ServiceAccount, @".\LocalSystem");
			}

			return true;
		}
	}
}