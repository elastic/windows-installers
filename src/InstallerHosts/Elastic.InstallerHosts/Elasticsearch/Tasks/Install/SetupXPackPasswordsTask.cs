using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetupXPackPasswordsTask : ElasticsearchInstallationTaskBase
	{
		public SetupXPackPasswordsTask(string[] args, ISession session) 
			: base(args, session) { }
		public SetupXPackPasswordsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var installationFolder = this.InstallationModel.LocationsModel.InstallDir;
			installationFolder = @"c:\Data\elasticsearch-6.0.0-beta2";
			var binary = this.FileSystem.Path.Combine(installationFolder, "bin", "x-pack", "setup-passwords") + ".bat";
			//if (!this.FileSystem.File.Exists(binary)) return false;

			var xPackModel = this.InstallationModel.XPackModel;
			if (!xPackModel.IsRelevant || !xPackModel.NeedsPasswords) return true;
			var p = new Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = binary,
					Arguments = "interactive",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
				}
			};
			
			ApproachOne(p);
			//ApproacheTwo(p);

			p.WaitForExit();

			var exitCode = p.ExitCode;
			p.Close();
			return true;
		}

		private void ApproacheTwo(Process p)
		{
			//p.StandardInput.BaseStream
			p.Start();
			
			var bufferSize = 1024;
			var read = 0;
			do
			{
				var buffer = new byte[bufferSize];
				for (read = 0; read < bufferSize; read++)
				{
					var eof = p.StandardOutput.EndOfStream;
					var exited = p.HasExited;
					
					var readByte = p.StandardOutput.BaseStream.ReadByte();
					if (readByte == -1)
						break;
					var c = (byte) readByte;
					if (c == '\n' || c == ']' || c == ':')
					{
						break;
					}
					
					buffer[read] = c;
				}
				var s = Encoding.UTF8.GetString(buffer, 0, read);
			} while (read > 0);
		}

		private void ApproachOne(Process p)
		{
			var errors = false;
			var steps = 0;
			
			p.ErrorDataReceived += (s, a) => errors = true;
			p.OutputDataReceived += (sender, a) =>
			{
				var message = a.Data;
				if (message == null) return;
				if (message.StartsWith("Exception")) errors = true;
				if (message.Trim().EndsWith("[y/N]"))
				{
					p.StandardInput.WriteLine("y");
				}
				if (string.IsNullOrEmpty(message.Trim()) && steps == 3)
				{
					p.StandardInput.WriteLine(this.InstallationModel.XPackModel.ElasticUserPassword);
				}
				steps++;
			};
			p.Start();
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
//			p.StandardInput.WriteLine(this.InstallationModel.XPackModel.ElasticUserPassword);
//			p.StandardInput.WriteLine(this.InstallationModel.XPackModel.KibanaUserPassword);
//			p.StandardInput.WriteLine(this.InstallationModel.XPackModel.KibanaUserPassword);
//			p.StandardInput.WriteLine(this.InstallationModel.XPackModel.LogstashSystemUserPassword);
//			p.StandardInput.WriteLine(this.InstallationModel.XPackModel.LogstashSystemUserPassword);
		}
	}
}