using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Configuration.Service;
using FluentValidation;
using ReactiveUI;
using Semver;

namespace Elastic.Installer.Domain.Model.Base.Closing
{
	public abstract class ClosingModelBase<TModel, TModelValidator> : StepBase<TModel, TModelValidator>
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		protected ClosingModelBase(
			SemVersion currentVersion,
			bool isUpgrade,
			IObservable<string> hostName,
			IObservable<string> wixLogFile,
			IObservable<string> productLog,
			IServiceStateProvider serviceStateProvider)
		{
			this.Header = "";
			this.CurrentVersion = currentVersion;
			this.Host = hostName;
			this.IsUpgrade = isUpgrade;
			this.WixLogFile = wixLogFile;
			this.ServiceStateProvider = serviceStateProvider;	
			this.OpenReference = ReactiveCommand.Create();
			this.OpenGettingStarted = ReactiveCommand.Create();
			this.OpenInstallationLog = ReactiveCommand.Create();
			this.OpenIssues = ReactiveCommand.Create();
			this.OpenProduct = ReactiveCommand.Create();
			this.OpenProductLog = ReactiveCommand.Create();
			this.ProductLog = productLog;
		}

		public SemVersion CurrentVersion { get; protected set; }
		public bool IsUpgrade { get; protected set; }
		public IObservable<string> WixLogFile { get; protected set; }
		public IServiceStateProvider ServiceStateProvider { get; protected set; }
		public ReactiveCommand<object> OpenReference { get; protected set; }
		public ReactiveCommand<object> OpenGettingStarted { get; protected set; }
		public ReactiveCommand<object> OpenInstallationLog { get; protected set; }
		public ReactiveCommand<object> OpenIssues { get; protected set; }
		public ReactiveCommand<object> OpenProductLog { get; protected set; }
		public ReactiveCommand<object> OpenProduct { get; protected set; }
		public IObservable<string> ProductLog { get; protected set; }
		public IObservable<string> Host { get; protected set; }

		ClosingResult? installed;
		public ClosingResult? Installed
		{
			get => installed;
			set => this.RaiseAndSetIfChanged(ref installed, value);
		}

		bool openDocs;
		[Argument(nameof(OpenDocumentationAfterInstallation))]
		public bool OpenDocumentationAfterInstallation
		{
			get => openDocs;
			set => this.RaiseAndSetIfChanged(ref openDocs, value);
		}

		IEnumerable<string> prerequisiteFailureMessages;
		public IEnumerable<string> PrerequisiteFailureMessages
		{
			get => prerequisiteFailureMessages;
			set => this.RaiseAndSetIfChanged(ref prerequisiteFailureMessages, value);
		}
	}
}
