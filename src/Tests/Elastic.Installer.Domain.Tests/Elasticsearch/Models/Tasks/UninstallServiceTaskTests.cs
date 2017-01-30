using System.IO;
using System.ServiceProcess;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using FluentAssertions;
using Xunit;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks
{
	public class UninstallServiceTaskTests : InstallationModelTestBase
	{
		[Fact] void UninstallByDefault()
		{
			var model = WithValidPreflightChecks();
			var serviceConfig = new NoopServiceStateProvider() { SeesService = true };
			model.AssertTask(
				(m, s, fs) => new UninstallServiceTask(m, s, fs, serviceConfig),
				(m, t) =>
				{
					var c = serviceConfig.SeenServiceConfig;
					c.Should().NotBeNull();
					c.ConfigDirectory.Should().Be(m.LocationsModel.ConfigDirectory);
					c.HomeDirectory.Should().Be(m.LocationsModel.InstallDir);
					c.ExeLocation.Should().Be(Path.Combine(m.LocationsModel.InstallDir, "bin", "elasticsearch.exe"));
					c.ServiceAccount.Should().Be(ServiceAccount.LocalSystem);
				}
			);
		}

		[Fact] void NotCalledWhenNoServiceIsSeen()
		{
			var model = WithValidPreflightChecks()
				.OnStep(m=>m.ServiceModel, s=> s.InstallAsService = false);
			var serviceConfig = new NoopServiceStateProvider() { SeesService = false };
			model.AssertTask(
				(m, s, fs) => new UninstallServiceTask(m, s, fs, serviceConfig),
				(m, t) =>
				{
					serviceConfig.SeenServiceConfig.Should().BeNull();
				}
			);
		}

		[Fact] void CalledWhenNotInstallingAsAServiceButSeenByController()
		{
			var model = WithValidPreflightChecks()
				.OnStep(m=>m.ServiceModel, s=> s.InstallAsService = false);
			var serviceConfig = new NoopServiceStateProvider() { SeesService = true };
			model.AssertTask(
				(m, s, fs) => new UninstallServiceTask(m, s, fs, serviceConfig),
				(m, t) =>
				{
					serviceConfig.SeenServiceConfig.Should().NotBeNull();
				}
			);
		}
	}
}
