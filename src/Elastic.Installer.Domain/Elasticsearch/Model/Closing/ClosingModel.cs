﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Elastic.Installer.Domain.Model;
using ReactiveUI;
using Semver;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Model.Closing;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Closing
{
	public class ClosingModel : ClosingModelBase<ClosingModel, ClosingModelValidator>
	{
		public ClosingModel(
			SemVersion currentVersion,
			bool isUpgrade,
			IObservable<string> hostName,
			IObservable<string> wixLogFile,
			IObservable<string> elasticsearchLog,
			IObservable<bool> installXPack,
			IServiceStateProvider serviceStateProvider) 
			: base(currentVersion, isUpgrade, hostName, wixLogFile, elasticsearchLog, installXPack, serviceStateProvider)
		{
			this.OpenFindYourClient = ReactiveCommand.Create();
			this.Refresh();
		}

		public sealed override void Refresh() { }

		public ReactiveCommand<object> OpenFindYourClient { get; }
	}
}