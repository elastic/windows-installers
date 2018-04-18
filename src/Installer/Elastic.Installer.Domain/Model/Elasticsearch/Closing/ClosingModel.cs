using System;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Model.Base.Closing;
using ReactiveUI;
using Semver;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Closing
{
	public class ClosingModel : ClosingModelBase<ClosingModel, ClosingModelValidator>
	{
		public ClosingModel(
			SemVersion currentVersion, 
			bool isUpgrade, 
			IObservable<string> hostName, 
			IObservable<string> wixLogFile, 
			IObservable<string> elasticsearchLog, 
			IServiceStateProvider serviceStateProvider) 
			: base(currentVersion, isUpgrade, hostName, wixLogFile, elasticsearchLog, serviceStateProvider)
		{
			this.OpenFindYourClient = ReactiveCommand.Create();
			this.Refresh();
		}

		public sealed override void Refresh() { }

		public ReactiveCommand<object> OpenFindYourClient { get; }
	}
}