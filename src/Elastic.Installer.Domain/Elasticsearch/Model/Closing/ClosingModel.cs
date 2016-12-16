using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Model;
using ReactiveUI;
using Semver;
using Elastic.Installer.Domain.Elasticsearch.Configuration;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Closing
{
	public class ClosingModel : StepBase<ClosingModel, ClosingModelValidator>
	{
		public ClosingModel(
			SemVersion currentVersion, 
			bool isUpgrade,
			IObservable<string> hostName, 
			IObservable<string> wixLogFile, 
			IObservable<string> elasticsearchLog,
			IServiceStateProvider serviceStateProvider)
		{
			this.Header = "";
			this.CurrentVersion = currentVersion;
			this.IsUpgrade = isUpgrade;
			this.Host = hostName;
			this.WixLogFile = wixLogFile;
			this.ElasticsearchLog = elasticsearchLog;
			this.OpenElasticsearch = ReactiveCommand.Create();
			this.OpenReference = ReactiveCommand.Create();
			this.OpenGettingStarted = ReactiveCommand.Create();
			this.OpenFindYourClient = ReactiveCommand.Create();
			this.OpenIssues = ReactiveCommand.Create();
			this.OpenInstallationLog = ReactiveCommand.Create();
			this.OpenElasticsearchLog = ReactiveCommand.Create();
			this.ServiceStateProvider = serviceStateProvider;
			this.Refresh();
		}

		public sealed override void Refresh() { }

		public SemVersion CurrentVersion { get; }
		public bool IsUpgrade { get; }
		public IObservable<string> Host { get; }
		public IObservable<string> WixLogFile { get; }
		public IObservable<string> ElasticsearchLog { get; }

		public ReactiveCommand<object> OpenElasticsearch { get; }
		public ReactiveCommand<object> OpenReference { get; }
		public ReactiveCommand<object> OpenGettingStarted { get; }
		public ReactiveCommand<object> OpenFindYourClient { get; }

		public ReactiveCommand<object> OpenInstallationLog { get; }
		public ReactiveCommand<object> OpenElasticsearchLog { get; }
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

		IEnumerable<string> prequisiteFailures;
		public IEnumerable<string> PrequisiteFailures
		{
			get { return prequisiteFailures; }
			set { this.RaiseAndSetIfChanged(ref prequisiteFailures, value); }
		}

	}
}