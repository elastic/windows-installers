using Elastic.Installer.Domain.Model;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Model.Configuration
{
	public class ConfigurationModel : StepBase<ConfigurationModel, ConfigurationModelValidator>
	{
		public const string DefaultHostName = "localhost";
		public const int DefaultPort = 5601;
		public static readonly string DefaultServerName = Environment.MachineName;
		public const string DefaultDefaultRoute = "/app/kibana";

		public ConfigurationModel()
		{
			this.Header = "Configuration";
		}

		string hostName;
		[StaticArgument(nameof(HostName))]
		public string HostName
		{
			get { return this.hostName; }
			set { this.RaiseAndSetIfChanged(ref this.hostName, value); }
		}
			
		int port;
		[StaticArgument(nameof(HttpPort))]
		public int HttpPort
		{
			get { return this.port; }
			set { this.RaiseAndSetIfChanged(ref this.port, value); }
		}

		string serverName;
		[StaticArgument(nameof(ServerName))]
		public string ServerName
		{
			get { return this.serverName; }
			set { this.RaiseAndSetIfChanged(ref this.serverName, value); }
		}

		string basePath;
		[StaticArgument(nameof(BasePath))]
		public string BasePath
		{
			get { return this.basePath; }
			set { this.RaiseAndSetIfChanged(ref this.basePath, value); }
		}

		string defaultRoute;
		[StaticArgument(nameof(DefaultRoute))]
		public string DefaultRoute
		{
			get { return this.defaultRoute; }
			set { this.RaiseAndSetIfChanged(ref this.defaultRoute, value); }
		}

		string serverKey;
		[StaticArgument(nameof(ServerKey))]
		public string ServerKey
		{
			get { return this.serverKey; }
			set { this.RaiseAndSetIfChanged(ref this.serverKey, value); }
		}

		string serverCertificate;
		[StaticArgument(nameof(ServerCertificate))]
		public string ServerCertificate
		{
			get { return this.serverCertificate; }
			set { this.RaiseAndSetIfChanged(ref this.serverCertificate, value); }
		}

		bool allowAnonymousAccess;
		[StaticArgument(nameof(AllowAnonymousAccess))]
		public bool AllowAnonymousAccess
		{
			get { return this.allowAnonymousAccess; }
			set { this.RaiseAndSetIfChanged(ref this.allowAnonymousAccess, value); }
		}

		public override void Refresh()
		{
			this.HostName = DefaultHostName;
			this.HttpPort = DefaultPort;
			this.ServerName = DefaultServerName;
			this.DefaultRoute = DefaultDefaultRoute;
		}
	}
}
