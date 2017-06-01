using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using FluentValidation.Results;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public class InstallationModelTester
	{
		public ElasticsearchInstallationModel InstallationModel { get; }
		public JavaConfiguration JavaConfig { get; }
		public ElasticsearchYamlConfiguration EsConfig { get; }
		public LocalJvmOptionsConfiguration JvmConfig { get; }
		public MockElasticsearchEnvironmentStateProvider EsState { get; private set; }
		public NoopPluginStateProvider PluginState { get; private set; }
		public MockJavaEnvironmentStateProvider JavaState { get; private set; }
		public MockFileSystem FileSystem { get; private set; }

		public InstallationModelTester() 
			: this
			(
				new MockWixStateProvider(),
				new MockJavaEnvironmentStateProvider(),
				new MockElasticsearchEnvironmentStateProvider(),
				new NoopServiceStateProvider(),
				new NoopPluginStateProvider(),
				new MockFileSystem(),
				new NoopSession(),
				null
			) { }

		public InstallationModelTester(
			MockWixStateProvider wixState, 
			MockJavaEnvironmentStateProvider javaState, 
			MockElasticsearchEnvironmentStateProvider esState, 
			NoopServiceStateProvider serviceState,
			NoopPluginStateProvider pluginState,
			MockFileSystem fileSystem, 
			NoopSession session,
			string[] args)
		{
			if (wixState == null) throw new ArgumentNullException(nameof(wixState));
			if (javaState == null) throw new ArgumentNullException(nameof(javaState));
			if (esState == null) throw new ArgumentNullException(nameof(esState));

			this.JavaState = javaState;
			this.EsState = esState;
			this.PluginState = pluginState;
			this.JavaConfig = new JavaConfiguration(javaState);
			var elasticsearchConfiguration = new ElasticsearchEnvironmentConfiguration(esState);
			this.EsConfig = ElasticsearchYamlConfiguration.FromFolder(elasticsearchConfiguration.ConfigDirectory, fileSystem);
			this.JvmConfig = LocalJvmOptionsConfiguration.FromFolder(elasticsearchConfiguration.ConfigDirectory, fileSystem);
			this.InstallationModel = new ElasticsearchInstallationModel(
				wixState, JavaConfig, elasticsearchConfiguration, serviceState, pluginState,  EsConfig, JvmConfig, session, args);
			this.FileSystem = fileSystem;
		}

		public InstallationModelTester IsInvalidOnStep(
			Func<ElasticsearchInstallationModel, IValidatableReactiveObject> selector,
			Action<IList<ValidationFailure>> validateErrors = null
			)
		{
			var step = selector(this.InstallationModel);
			return IsInvalidOnStep(validateErrors, step);
		}

		public InstallationModelTester HasPrerequisiteErrors(Action<IList<ValidationFailure>> validateErrors = null)
		{
			var firstStep = this.InstallationModel.Steps.First();
			this.InstallationModel.ActiveStep.Should().Be(firstStep);

			this.InstallationModel.PrerequisiteFailures.Should().NotBeEmpty();
			validateErrors?.Invoke(this.InstallationModel.PrerequisiteFailures);

			return this;
		}

		public InstallationModelTester HasSetupValidationFailures(Action<IList<ValidationFailure>> validateErrors = null)
		{
			var firstStep = this.InstallationModel.Steps.First();
			this.InstallationModel.ActiveStep.Should().Be(firstStep);

			this.InstallationModel.ValidationFailures.Should().NotBeEmpty();
			validateErrors?.Invoke(this.InstallationModel.ValidationFailures);

			return this;
		}


		public InstallationModelTester IsInvalidOnStep(Action<IList<ValidationFailure>> validateErrors, IValidatableReactiveObject step)
		{
			this.InstallationModel.ActiveStep.Should().Be(step);
			step.IsValid.Should().BeFalse("{0} should be invalid", step.GetType().Name);

			this.InstallationModel.CurrentStepValidationFailures.Should().NotBeEmpty();
			step.ValidationFailures.Should().NotBeEmpty()
				.And.HaveCount(this.InstallationModel.CurrentStepValidationFailures.Count);

			validateErrors?.Invoke(step.ValidationFailures);

			return this;
		}

		public InstallationModelTester IsValidOnFirstStep()
		{
			var firstStep = this.InstallationModel.Steps.First();
			return IsValidOnStep(firstStep);
		}

		public InstallationModelTester IsNotified(Action<NoticeModel> assert)
		{
			var step = this.InstallationModel.NoticeModel;
			this.InstallationModel.ActiveStep.Should().Be(step);

			assert?.Invoke(step);
			return this;
		}

		public InstallationModelTester IsClosing(Action<ClosingModel> assert)
		{
			var step = this.InstallationModel.ClosingModel;
			this.InstallationModel.ActiveStep.Should().Be(step);

			assert?.Invoke(step);
			return this;
		}

		public InstallationModelTester IsValidOnStep(Func<ElasticsearchInstallationModel, IValidatableReactiveObject> selector)
		{
			var step = selector(this.InstallationModel);
			return IsValidOnStep(step);
		}

		public InstallationModelTester IsValidOnStep(IValidatableReactiveObject step)
		{
			this.InstallationModel.ActiveStep.Should().Be(step);
			step.IsValid.Should().BeTrue("error messages: {0}", this.InstallationModel.CurrentStepValidationFailures.ToUnitTestMessage());

			this.InstallationModel.CurrentStepValidationFailures.Should().BeEmpty();
			step.ValidationFailures.Should().BeEmpty();

			return this;
		}

		public InstallationModelTester CanClickNext(bool canClick = true)
		{
			var c = false;
			this.InstallationModel.Next.CanExecuteObservable.Subscribe(cc => c = cc);
			c.Should().Be(canClick);
			return this;
		}

		public InstallationModelTester ClickNext()
		{
			CanClickNext();
			this.InstallationModel.Next.Execute(null);
			return this;
		}

		public InstallationModelTester CanClickBack(bool canClick = true)
		{
			var c = false;
			this.InstallationModel.Back.CanExecuteObservable.Subscribe(cc => c = cc);
			c.Should().Be(canClick);
			return this;
		}

		public InstallationModelTester ClickBack()
		{
			CanClickNext();
			this.InstallationModel.Back.Execute(null);
			return this;
		}

		public InstallationModelTester ClickRefresh()
		{
			this.InstallationModel.RefreshCurrentStep.Execute(null);
			return this;
		}


		public InstallationModelTester OnStep<TStep>(
			Func<ElasticsearchInstallationModel, TStep> selector,
			Action<TStep> modify
			)
			where TStep : IValidatableReactiveObject
		{
			var step  = selector(this.InstallationModel);
			modify(step);

			return this;
		}

		public static InstallationModelTester ValidPreflightChecks() => New(s => s
			.Wix(alreadyInstalled: false)
			.Java(j=>j.JavaHomeMachineVariable("C:\\Java"))
		);

		public static InstallationModelTester ValidPreflightChecks(Func<TestSetupStateProvider, TestSetupStateProvider> selector) => New(s => selector(s
			.Wix(alreadyInstalled: false)
			.Java(j => j.JavaHomeMachineVariable("C:\\Java"))
			)
		);

		public void AssertTask<TTask>(Func<ElasticsearchInstallationModel, ISession, MockFileSystem, TTask> createTask, Action<ElasticsearchInstallationModel, InstallationModelTester> assert)
			where TTask : ElasticsearchInstallationTask
		{
			var task = createTask(this.InstallationModel, new NoopSession(), this.FileSystem);
			Action a = () => task.Execute();
			a.ShouldNotThrow();
			assert(this.InstallationModel, this);
		}

		public static InstallationModelTester New(Func<TestSetupStateProvider, TestSetupStateProvider> selector)
		{
			var setupState = selector(new TestSetupStateProvider());
			return new InstallationModelTester(
				setupState.WixState,
				setupState.JavaState,
				setupState.ElasticsearchState,
				setupState.ServiceState,
				setupState.PluginState,
				setupState.FileSystemState,
				setupState.SessionState,
				setupState.Arguments);
		}

		public static InstallationModelTester New()
		{
			return new InstallationModelTester();
		}
	}
}