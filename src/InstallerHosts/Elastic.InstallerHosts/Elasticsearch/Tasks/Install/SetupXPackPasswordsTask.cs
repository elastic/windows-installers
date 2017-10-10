using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
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
			var xPackModel = this.InstallationModel.XPackModel;
			if (!xPackModel.IsRelevant || !xPackModel.NeedsPasswords) return true;

			var password = this.InstallationModel.XPackModel.BootstrapPassword;

			using (var client = new HttpClient())
			{
				var elasticUserPassword = this.InstallationModel.XPackModel.ElasticUserPassword;

				if (!string.IsNullOrEmpty(elasticUserPassword))
				{
					SetPassword(client, password, "elastic", elasticUserPassword);
					this.Session.Log("Changed password for user [elastic]");
				}

				// change the elastic user password to use for subsequent users after updating it for elastic user
				password = !string.IsNullOrEmpty(elasticUserPassword)
					? elasticUserPassword
					: password;

				if (!string.IsNullOrEmpty(this.InstallationModel.XPackModel.KibanaUserPassword))
				{
					SetPassword(client, password, "kibana",  this.InstallationModel.XPackModel.KibanaUserPassword);
					this.Session.Log("Changed password for user [kibana]");
				}

				if (!string.IsNullOrEmpty(this.InstallationModel.XPackModel.LogstashSystemUserPassword))
				{
					SetPassword(client, password, "logstash_system", this.InstallationModel.XPackModel.LogstashSystemUserPassword);
					this.Session.Log("Changed password for user [logstash_system]");
				}
			}
			
			return true;
		}

		private void SetPassword(HttpClient client, string elasticUserPassword, string user, string password)
		{
			var host = this.InstallationModel.ConfigurationModel.NetworkHost ?? "localhost";
			var port = this.InstallationModel.ConfigurationModel.HttpPort;

			using (var message = new HttpRequestMessage(HttpMethod.Put, $"http://{host}:{port}/_xpack/security/user/{user}/_password"))
			{
				var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{elasticUserPassword}"));

				message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
				message.Content = new StringContent($"{{\"password\":\"{password}\"}}", Encoding.UTF8, "application/json");
				var response = client.SendAsync(message).Result;
				response.EnsureSuccessStatusCode();
			}
		}
	}
}