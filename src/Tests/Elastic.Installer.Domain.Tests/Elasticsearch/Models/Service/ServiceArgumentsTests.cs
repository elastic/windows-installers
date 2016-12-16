using Elastic.Installer.Domain.Elasticsearch.Model.Service;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Service
{
	public class ServiceArgumentsTests : InstallationModelArgumentsTestsBase
	{

		[Fact] void Password() => Argument(nameof(ServiceModel.Password), "MyPa$$", (m, v) =>
		{
			m.ServiceModel.Password.Should().Be(v);
			m.ServiceModel.UseExistingUser.Should().BeTrue();
		});

		[Fact] void User() => Argument(nameof(ServiceModel.User), "MyPa$$", (m, v) =>
		{
			m.ServiceModel.User.Should().Be(v);
			m.ServiceModel.UseExistingUser.Should().BeTrue();
		});

		[Fact] void InstallAsService() => Argument(nameof(ServiceModel.InstallAsService), true, (m, v) =>
		{
			m.ServiceModel.InstallAsService.Should().Be(v);
		});

		[Fact] void DontInstallAsService() => Argument(nameof(ServiceModel.InstallAsService), false, (m, v) =>
		{
			m.ServiceModel.InstallAsService.Should().Be(v);
		});

		[Fact] void DontInstallAsServiceFalsy() => Argument(nameof(ServiceModel.InstallAsService), "0", "false", (m, v) =>
		{
			m.ServiceModel.InstallAsService.Should().BeFalse();
		});

		[Fact] void DontInstallAsServiceTruthy() => Argument(nameof(ServiceModel.InstallAsService), "1", "true", (m, v) =>
		{
			m.ServiceModel.InstallAsService.Should().BeTrue();
		});

		[Fact] void StartAfterInstall() => Argument(nameof(ServiceModel.StartAfterInstall), true, (m, v) =>
		{
			m.ServiceModel.StartAfterInstall.Should().Be(v);
		});

		[Fact] void StartWhenWindowsStarts() => Argument(nameof(ServiceModel.StartWhenWindowsStarts), true, (m, v) =>
		{
			m.ServiceModel.StartWhenWindowsStarts.Should().Be(v);
		});

		[Fact] void UseExistingUser() => Argument(nameof(ServiceModel.UseExistingUser), true, (m, v) =>
		{
			m.ServiceModel.UseExistingUser.Should().Be(v);
		});

		[Fact] void UseLocalSystem() => Argument(nameof(ServiceModel.UseLocalSystem), true, (m, v) =>
		{
			m.ServiceModel.UseLocalSystem.Should().Be(v);
		});

		[Fact] void UseNetworkService() => Argument(nameof(ServiceModel.UseNetworkService), true, (m, v) =>
		{
			m.ServiceModel.UseNetworkService.Should().Be(v);
		});
	}
}
