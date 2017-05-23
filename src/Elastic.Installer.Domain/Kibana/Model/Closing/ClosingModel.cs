using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Model;
using ReactiveUI;
using Semver;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Model.Closing;

namespace Elastic.Installer.Domain.Kibana.Model.Closing
{
	public class ClosingModel : ClosingModelBase<ClosingModel, ClosingModelValidator>
	{
		public ClosingModel(
			SemVersion currentVersion, 
			bool isUpgrade,
			IObservable<string> hostName, 
			IObservable<string> wixLogFile, 
			IObservable<string> kibanaLog,
			IObservable<bool> installXPack, 
			IServiceStateProvider serviceStateProvider)
			: base(currentVersion, isUpgrade, hostName, wixLogFile, kibanaLog, installXPack, serviceStateProvider)
		{
			this.Refresh();
		}

		public sealed override void Refresh() { }
	}
}