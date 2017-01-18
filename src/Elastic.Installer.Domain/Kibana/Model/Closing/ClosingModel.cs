using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Model;
using ReactiveUI;
using Semver;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Model.Closing;

namespace Elastic.Installer.Domain.Kibana.Model.Closing
{
	public class ClosingModel : StepBase<ClosingModel, ClosingModelValidator>
	{
		public ClosingModel(
			SemVersion currentVersion, 
			bool isUpgrade,
			IObservable<string> hostName, 
			IObservable<string> wixLogFile, 
			IServiceStateProvider serviceStateProvider)
		{
			this.Header = "";
			this.CurrentVersion = currentVersion;
			this.IsUpgrade = isUpgrade;
			this.Host = hostName;
			this.WixLogFile = wixLogFile;
			this.OpenKibana = ReactiveCommand.Create();
			this.OpenReference = ReactiveCommand.Create();
			this.OpenGettingStarted = ReactiveCommand.Create();
			this.OpenIssues = ReactiveCommand.Create();
			this.OpenInstallationLog = ReactiveCommand.Create();
			this.ServiceStateProvider = serviceStateProvider;
			this.Refresh();
		}

		public sealed override void Refresh() { }

		public SemVersion CurrentVersion { get; }
		public bool IsUpgrade { get; }
		public IObservable<string> Host { get; }
		public IObservable<string> WixLogFile { get; }

		public ReactiveCommand<object> OpenKibana { get; }
		public ReactiveCommand<object> OpenReference { get; }
		public ReactiveCommand<object> OpenGettingStarted { get; }
		public ReactiveCommand<object> OpenInstallationLog { get; }
		public ReactiveCommand<object> OpenIssues { get; }

		public IServiceStateProvider ServiceStateProvider { get; }

		// ReactiveUI conventions do not change
		// ReSharper disable InconsistentNaming
		// ReSharper disable ArrangeTypeMemberModifiers
		ClosingResult? installed;
		public ClosingResult? Installed
		{
			get { return installed; }
			set { this.RaiseAndSetIfChanged(ref installed, value); }
		}

		bool openDocs;
		[Argument(nameof(OpenDocumentationAfterInstallation))]
		public bool OpenDocumentationAfterInstallation
		{
			get { return openDocs; }
			set { this.RaiseAndSetIfChanged(ref openDocs, value); }
		}

		IEnumerable<string> prerequisiteFailureMessages;
		public IEnumerable<string> PrerequisiteFailureMessages
		{
			get { return prerequisiteFailureMessages; }
			set { this.RaiseAndSetIfChanged(ref prerequisiteFailureMessages, value); }
		}

	}
}