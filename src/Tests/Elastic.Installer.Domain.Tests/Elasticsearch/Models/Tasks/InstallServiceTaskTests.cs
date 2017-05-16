using System.IO;
using System.ServiceProcess;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using FluentAssertions;
using Xunit;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks
{
	public class InstallServiceTaskTests : InstallationModelTestBase
	{
		[Fact] void InstallByDefault()
		{
			var model = WithValidPreflightChecks();
			var serviceConfig = new NoopServiceStateProvider();
			model.AssertTask(
				(m, s, fs) => new InstallServiceTask(m, s, fs, serviceConfig),
				(m, t) =>
				{
					var c = serviceConfig.SeenServiceConfig;
					c.Should().NotBeNull();
					c.HomeDirectory.Should().Be(m.LocationsModel.InstallDir);
					c.ConfigDirectory.Should().Be(m.LocationsModel.ConfigDirectory);
					c.ExeLocation.Should().Be(Path.Combine(m.LocationsModel.InstallDir, "bin", "elasticsearch.exe"));
					c.ServiceAccount.Should().Be(ServiceAccount.LocalSystem);
				}
			);
		}

		[Fact] void InstallSuppliedUsernameAndPassword()
		{
			var model = WithValidPreflightChecks()
				.OnStep(m=>m.ServiceModel, s=>
				{
					s.User = "mpdreamz";
					s.Password = "my super pazz";
					s.StartWhenWindowsStarts = false;
				});
				;
			var serviceConfig = new NoopServiceStateProvider();
			model.AssertTask(
				(m, s, fs) => new InstallServiceTask(m, s, fs, serviceConfig),
				(m, t) =>
				{
					var c = serviceConfig.SeenServiceConfig;
					c.Should().NotBeNull();
					c.ConfigDirectory.Should().Be(m.LocationsModel.ConfigDirectory);
					c.HomeDirectory.Should().Be(m.LocationsModel.InstallDir);
					c.ExeLocation.Should().Be(Path.Combine(m.LocationsModel.InstallDir, "bin", "elasticsearch.exe"));
					c.ServiceAccount.Should().Be(ServiceAccount.User);
					c.StartMode.Should().Be(ServiceStartMode.Manual);
					c.UserName.Should().Be("mpdreamz");
					c.Password.Should().Be("my super pazz");
				}
			);
		}


		[Fact] void NotCalledWhenNotInstallingAsService()
		{
			var model = WithValidPreflightChecks()
				.OnStep(m=>m.ServiceModel, s=> s.InstallAsService = false);
			var serviceConfig = new NoopServiceStateProvider();
			model.AssertTask(
				(m, s, fs) => new InstallServiceTask(m, s, fs, serviceConfig),
				(m, t) =>
				{
					serviceConfig.SeenServiceConfig.Should().BeNull();
				}
			);
		}
	}
}
