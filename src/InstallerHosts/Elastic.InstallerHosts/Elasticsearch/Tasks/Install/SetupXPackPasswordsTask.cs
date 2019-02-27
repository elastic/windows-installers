using System;
using System.IO.Abstractions;
using System.Linq;
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
		private const int DefaultHttpPort = 9200;

		// https://www.elastic.co/guide/en/elasticsearch/reference/current/modules-network.html#network-interface-values
		private const string LocalhostPlaceholder = "_local_";
		private const string LocalhostV4Placeholder = "_local:ipv4_";
		private const string LocalhostV6Placeholder = "_local:ipv6_";
		private const string SiteAddressPlaceholder = "_site_";
		private const string SiteAddressV4Placeholder = "_site:ipv4_";
		private const string SiteAddressV6Placeholder = "_site:ipv6_";
		private const string GlobalAddressPlaceholder = "_global_";
		private const string GlobalAddressV4Placeholder = "_global:ipv4_";
		private const string GlobalAddressV6Placeholder = "_global:ipv6_";

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Setting up X-Pack passwords", "Setting up X-Pack passwords: [1]");

			var password = this.InstallationModel.XPackModel.BootstrapPassword;
			var baseAddress = GetBaseAddress(this.InstallationModel.ConfigurationModel.NetworkHost, this.InstallationModel.ConfigurationModel.HttpPort);

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

		// Might be moved and refactored when is needed for other tasks
		public static string GetBaseAddress(string networkHost, int? httpPort)
		{
			var port = httpPort ?? DefaultHttpPort;

			if (string.IsNullOrEmpty(networkHost))
				return $"http://localhost:{port}/";

			var host = networkHost;
			if (networkHost.IndexOf(',') >= 0) // Uri host must not contain comma, but elasticsearch.yml supports it
			{
				var hosts = networkHost.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim());
				host = hosts.FirstOrDefault(h => h == "localhost" || h == "127.0.0.1"
					|| h == LocalhostPlaceholder || h == LocalhostV4Placeholder || h == LocalhostV6Placeholder) // prefer local interface
					?? hosts.First();
			}

			switch (host)
			{
				case LocalhostPlaceholder:
					host = "localhost";
					break;
				case LocalhostV4Placeholder:
					host = "127.0.0.1";
					break;
				case LocalhostV6Placeholder:
					host = "[::1]";
					break;
				//TODO: differentiate between _site_ and _global_
				case SiteAddressPlaceholder:
				case GlobalAddressPlaceholder:
				case SiteAddressV4Placeholder:
				case GlobalAddressV4Placeholder:
				case SiteAddressV6Placeholder:
				case GlobalAddressV6Placeholder:
					IPHostEntry ipHostEntry = Dns.GetHostEntry(Dns.GetHostName());
					var ipAddresses = ipHostEntry.AddressList
						.Where(a => !IPAddress.IsLoopback(a) && (a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6));
					if (host == SiteAddressV4Placeholder || host == GlobalAddressV4Placeholder)
						ipAddresses = ipAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork);
					if (host == SiteAddressV6Placeholder || host == GlobalAddressV6Placeholder)
						ipAddresses = ipAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6);
					var ipAddress = ipAddresses.First();
					host = ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{ipAddress}]" : ipAddress.ToString();
					break;
			}

			// do not remove trailing slash. Base address *must* have it
			var baseAddress = $"http://{host}:{port}/";
			if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
			{
				// uri is not right, as installation is always local it's better to try localhost anyway
				baseAddress = $"http://localhost:{port}/";
			}

			return baseAddress;
		}
	}
}