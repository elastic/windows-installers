using System;
using System.Linq;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.Domain.Model.Kibana.Plugins;
using Elastic.Installer.Domain.Properties;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public class InstallationModelTests : InstallationModelTestBase
	{
		/* Instantiatiing a new InstallationModel will be invalid without 
		 * Setting some preflight checks */
		[Fact] void PreflightChecks() => Pristine()
			.HasSetupValidationFailures(errors=>errors
				.ShouldHaveErrors(
					TextResources.NoticeModelValidator_JavaInstalled
				)
			)
			//Java is a fixable prequisite error
			.HasPrerequisiteErrors(errors=>errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaInstalled)
			)
			.IsValidOnFirstStep()
			//Assure we can not proceed past the current step
			.CanClickNext(true);
	
		/* A model with all preflight checks set is valid */
		[Fact] void CanClickNextWhenValid() => WithValidPreflightChecks()
			.IsValidOnFirstStep()
			.CanClickNext();

		/* When the first step is valid and we go to the next step and back
		* the model should still be valid and not reset */
		[Fact] public void ClickingBackDoesNotReset() => WithValidPreflightChecks()
			.IsValidOnFirstStep()
			.ClickNext()
			.IsValidOnStep(m => m.ServiceModel)
			.ClickBack()
			.IsValidOnFirstStep()
			.CanClickNext()
			.CanClickBack(false);


		/** Make sure that the active step never exceeds the first step with errors */
		[Fact] public void ActiveStepFollowsFirstViewModelWithErrors() => WithValidPreflightChecks()
			.IsValidOnFirstStep()
			.ClickNext()
			.ClickNext()
			.IsValidOnStep(m => m.ConfigurationModel)
			.OnStep(m => m.LocationsModel, step => step.DataDirectory = null)
			.IsInvalidOnStep(m => m.LocationsModel)
			.CanClickNext(false);
		
		/** If all the preflight checks are set and we've accepted the license 
		All the other step models are valid by default and we can click next untill we get to the install phase */
		[Fact] public void GivenValidPreflightChecksCanClickNextTillInstall()
		{
			var tester = WithValidPreflightChecks();
			tester.IsValidOnFirstStep();
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_NextText);
			tester.ClickNext();

			tester.IsValidOnStep(m => m.ServiceModel);
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_NextText);
			tester.ClickNext();

			tester.IsValidOnStep(m => m.ConfigurationModel);
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_NextText);
			tester.ClickNext();
		
			tester.IsValidOnStep(m => m.PluginsModel);
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_InstallText);
			tester.ClickNext();
		}
		
		[Fact] public void HappyFlowWithXPackMakesItToInstallByOnlyClickingNext()
		{
			var tester = WithValidPreflightChecks();
			tester.IsValidOnFirstStep();
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_NextText);
			tester.ClickNext();

			tester.IsValidOnStep(m => m.ServiceModel);
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_NextText);
			tester.ClickNext();

			tester.IsValidOnStep(m => m.ConfigurationModel);
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_NextText);
			tester.ClickNext();

			tester.IsValidOnStep(m => m.PluginsModel);
			tester.InstallationModel.XPackModel.IsRelevant.Should().BeFalse();
			var step = tester.InstallationModel.PluginsModel;
			var xpackPlugin = step.AvailablePlugins.First(p => p.PluginType == PluginType.XPack);
			xpackPlugin.Selected = true;
			tester.InstallationModel.XPackModel.IsRelevant.Should().BeTrue();
			
			tester.ClickNext();
			tester.IsInvalidOnStep(m => m.XPackModel, errors => errors.ShouldHaveErrors(
				"Password is required"
			));

			tester.InstallationModel.XPackModel.ElasticUserPassword = Guid.NewGuid().ToString();

			tester.CanClickNext();
		
			tester.InstallationModel.NextButtonText.Should().Be(TextResources.SetupView_InstallText);
			tester.ClickNext();
		}
	}
}
