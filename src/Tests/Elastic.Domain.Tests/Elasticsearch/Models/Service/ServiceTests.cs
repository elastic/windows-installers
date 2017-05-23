using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Service
{
	public class ServiceTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public ServiceTests()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.IsValidOnStep(m => m.ServiceModel);
		}

		[Fact] void InstallingAsAServiceByDefault() => this._model
			.OnStep(m => m.ServiceModel, step => 
			{
				step.InstallAsService.Should().BeTrue();
			})
			.CanClickNext();

			
		[Fact] void SpecifiyingDoNotInstallResets() => this._model
			.OnStep(m => m.ServiceModel, step => 
			{
				step.UseLocalSystem.Should().BeTrue();
				step.User = "Mpdreamz";
				step.UseExistingUser.Should().BeTrue();
				step.InstallAsService = false;
				step.User.Should().BeNullOrEmpty();
				step.UseLocalSystem.Should().BeTrue();
			})
			.CanClickNext();
	}
}
