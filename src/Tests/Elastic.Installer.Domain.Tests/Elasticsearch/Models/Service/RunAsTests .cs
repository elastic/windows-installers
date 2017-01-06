using Elastic.Installer.Domain.Shared.Model.Service;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Service
{
	public class RunAsTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public RunAsTests()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.IsValidOnStep(m => m.ServiceModel);
		}

		[Fact] void SpecifyingMultipleTypesIsInvalid() => this._model
			.OnStep(m => m.ServiceModel, step =>
			{
				step.UseLocalSystem = true;
				step.UseNetworkService = true;
			})
			.IsInvalidOnStep(m => m.ServiceModel, errors => errors.ShouldHaveErrors(
				ServiceModelValidator.MultipleRunAsTypes
			));

		[Fact] void SpecifyingMultipleTypesIsInvalidDifferentCombination() => this._model
			.OnStep(m => m.ServiceModel, step =>
			{
				step.UseExistingUser = true;
				step.UseNetworkService = true;
			})
			.IsInvalidOnStep(m => m.ServiceModel, errors => errors.ShouldHaveErrors(
				ServiceModelValidator.UserNotEmptyWhenUseExistingUser,
				ServiceModelValidator.PasswordNotEmptyWhenUseExistingUser,
				ServiceModelValidator.MultipleRunAsTypes
			));

		[Fact] void WhenSpecifyingUserTypeSwitchesAndPasswordStillNeedsToBeFilledIn() => this._model
			.OnStep(m => m.ServiceModel, step =>
			{
				step.UseLocalSystem.Should().BeTrue();
				step.User = "Mpdreamz";
				step.UseExistingUser.Should().BeTrue();
				step.UseLocalSystem.Should().BeFalse();
				step.UseNetworkService.Should().BeFalse();
			})
			.IsInvalidOnStep(m => m.ServiceModel, errors => errors.ShouldHaveErrors(
				ServiceModelValidator.PasswordNotEmptyWhenUseExistingUser
			))
			.OnStep(m => m.ServiceModel, step =>
			{
				step.Password = "pazz";
			})
			.IsValidOnStep(m => m.ServiceModel);

		[Fact] void WhenSpecifyingPasswordTypeSwitchesAndUserStillNeedsToBeFilledIn() => this._model
			.OnStep(m => m.ServiceModel, step =>
			{
				step.UseLocalSystem.Should().BeTrue();
				step.Password = "pazz";
				step.UseExistingUser.Should().BeTrue();
				step.UseLocalSystem.Should().BeFalse();
				step.UseNetworkService.Should().BeFalse();
			})
			.IsInvalidOnStep(m => m.ServiceModel, errors => errors.ShouldHaveErrors(
				ServiceModelValidator.UserNotEmptyWhenUseExistingUser
			))
			.OnStep(m => m.ServiceModel, step =>
			{
				step.User = "mpdreamz";
			})
			.IsValidOnStep(m => m.ServiceModel);

	}
}
