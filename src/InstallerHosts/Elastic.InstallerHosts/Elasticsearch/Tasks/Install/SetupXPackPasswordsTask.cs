using System;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

		private const int TotalTicks = 1200;

		protected override bool ExecuteTask()
		{
			var xPackModel = this.InstallationModel.XPackModel;
			if (!xPackModel.IsRelevant || !xPackModel.NeedsPasswords) return true;

			this.Session.SendActionStart(TotalTicks, ActionName, "Setting up X-Pack passwords", "Setting up X-Pack passwords: [1]");

			var password = this.InstallationModel.XPackModel.BootstrapPassword;
			var host = !string.IsNullOrEmpty(this.InstallationModel.ConfigurationModel.NetworkHost)
				? this.InstallationModel.ConfigurationModel.NetworkHost
				: "localhost";
			var port = this.InstallationModel.ConfigurationModel.HttpPort;
			// do not remove trailing slash. Base address *must* have it
			var baseAddress = $"http://{host}:{port}/";

			using (var client = new HttpClient { BaseAddress = new Uri(baseAddress) })
			{
				WaitForNodeToAcceptRequests(client, password);
				var elasticUserPassword = this.InstallationModel.XPackModel.ElasticUserPassword;
				SetPassword(client, password, "elastic", elasticUserPassword);
				// change the elastic user password used for subsequent users, after updating it for elastic user
				password = elasticUserPassword;
				SetPassword(client, password, "kibana", this.InstallationModel.XPackModel.KibanaUserPassword);
				SetPassword(client, password, "logstash_system", this.InstallationModel.XPackModel.LogstashSystemUserPassword);
			}
			
			return true;
		}

		private void WaitForNodeToAcceptRequests(HttpClient client, string elasticUserPassword)
		{
			var statusCode = 500;
			var times = 0;
			var totalTimes = 30;
			var sleepyTime = 1000;
			var totalTicks = 300;
			var tickIncrement = totalTicks / totalTimes;

			do
			{
				if (times > 0) Thread.Sleep(sleepyTime);
				HttpResponseMessage response = null;
				try
				{
					this.Session.SendProgress(tickIncrement, "Checking Elasticsearch is up and running");

					using (var message = new HttpRequestMessage(HttpMethod.Head, string.Empty))
					{
						var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{elasticUserPassword}"));
						message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
						response = client.SendAsync(message).Result;
						statusCode = (int)response.StatusCode;
					}
				}
				catch (AggregateException ae)
				{
					if (response != null)
						statusCode = (int) response.StatusCode;

					var httpRequestException = ae.InnerException as HttpRequestException;
					if (httpRequestException == null) throw;

					var webException = httpRequestException.InnerException as WebException;
					if (webException == null) throw;

					var socketException = webException.InnerException as SocketException;
					if (socketException == null) throw;

					if (socketException.SocketErrorCode != SocketError.ConnectionRefused) throw;
				}

				++times;
			} while (statusCode >= 500 && times < totalTimes);

			if (statusCode >= 500)
				throw new TimeoutException($"Elasticsearch not seen running after trying for {TimeSpan.FromMilliseconds(totalTimes * sleepyTime)}");

			this.Session.SendProgress(totalTicks - (tickIncrement * times), "Elasticsearch is up and running");
		}

		private void SetPassword(HttpClient client, string elasticUserPassword, string user, string password)
		{
			this.Session.SendProgress(100, $"Changing password for '{user}'");
			using (var message = new HttpRequestMessage(HttpMethod.Put, $"_xpack/security/user/{user}/_password"))
			{
				var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{elasticUserPassword}"));

				message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
				message.Content = new StringContent($"{{\"password\":\"{password}\"}}", Encoding.UTF8, "application/json");
				var response = client.SendAsync(message).Result;
				response.EnsureSuccessStatusCode();
			}
			this.Session.SendProgress(200, $"Changed password for user '{user}'");
		}
	}
}