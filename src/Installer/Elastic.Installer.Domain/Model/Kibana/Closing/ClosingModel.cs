using System;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Model.Base.Closing;
using Semver;

namespace Elastic.Installer.Domain.Model.Kibana.Closing
{
	public class ClosingModel : ClosingModelBase<ClosingModel, ClosingModelValidator>
	{
		public ClosingModel(
			SemVersion currentVersion, 
			bool isUpgrade,
			IObservable<string> hostName, 
			IObservable<string> wixLogFile, 
			IObservable<string> kibanaLog,
			IServiceStateProvider serviceStateProvider)
			: base(currentVersion, isUpgrade, hostName, wixLogFile, kibanaLog, serviceStateProvider)
		{
			this.Refresh();
		}

		public sealed override void Refresh() { }
	}
}