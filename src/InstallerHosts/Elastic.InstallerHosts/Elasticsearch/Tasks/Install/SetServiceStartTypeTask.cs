using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetServiceStartTypeTask : ElasticsearchInstallationTaskBase
	{
		public SetServiceStartTypeTask(string[] args, ISession session) 
			: base(args, session) { }

		public SetServiceStartTypeTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var p = new Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = "sc.exe",
					Arguments = $"config {ServiceModel.ElasticsearchServiceName} start= demand",
					ErrorDialog = false,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true
				}
			};

			void OnDataReceived(object sender, DataReceivedEventArgs a)
			{
				var message = a.Data;
				if (message != null) this.Session.Log(message);
			}

			var errors = false;

			void OnErrorsReceived(object sender, DataReceivedEventArgs a)
			{
				var message = a.Data;
				if (message != null)
				{
					errors = true;
					this.Session.Log(message);
				}
			}

			p.ErrorDataReceived += OnErrorsReceived;
			p.OutputDataReceived += OnDataReceived;
			p.Start();
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			p.WaitForExit();

			var exitCode = p.ExitCode;
			if (exitCode != 0)
				this.Session.Log($"sc.exe process returned non-zero exit code: {exitCode}");

			p.ErrorDataReceived -= OnErrorsReceived;
			p.OutputDataReceived -= OnDataReceived;
			p.Close();

			return !errors && exitCode == 0;
		}
	}
}