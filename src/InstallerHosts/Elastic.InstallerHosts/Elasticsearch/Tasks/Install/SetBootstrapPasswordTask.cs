using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetBootstrapPasswordTask : ElasticsearchInstallationTaskBase
	{
		public SetBootstrapPasswordTask(string[] args, ISession session) 
			: base(args, session) {}

		public SetBootstrapPasswordTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			var xPackModel = this.InstallationModel.XPackModel;
			if (!xPackModel.IsRelevant || !xPackModel.XPackSecurityEnabled || xPackModel.XPackLicense != XPackLicenseMode.Trial) return true;

			var installationDir = this.InstallationModel.LocationsModel.InstallDir;
			var password = this.InstallationModel.XPackModel.BootstrapPassword;
			var binary = this.FileSystem.Path.Combine(installationDir, "bin", "elasticsearch-keystore.bat");

			var p = new Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = binary,
					Arguments = "add -x -f",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					EnvironmentVariables =
					{
						{ ElasticsearchEnvironmentStateProvider.ConfDir, this.InstallationModel.LocationsModel.ConfigDirectory }
					}
				}
			};

			void OnDataReceived(object sender, DataReceivedEventArgs a)
			{
				var message = a.Data;
				if (message != null)
					this.Session.Log(message);
			}

			p.ErrorDataReceived += OnDataReceived;
			p.OutputDataReceived += OnDataReceived;
			p.Start();

			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			p.StandardInput.WriteLine(password);
			p.WaitForExit();

			p.ErrorDataReceived -= OnDataReceived;
			p.OutputDataReceived -= OnDataReceived;
			p.Close();

			return true;
		}
	}
}