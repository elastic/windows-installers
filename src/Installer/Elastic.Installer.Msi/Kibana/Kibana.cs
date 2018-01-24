using System;
using System.Collections.Generic;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Kibana;

namespace Elastic.Installer.Msi.Kibana
{
	public class Kibana : Product
	{
		public override IEnumerable<string> AllArguments => KibanaArgumentParser.AllArguments;

		public override IEnumerable<ModelArgument> MsiParams =>
			KibanaInstallationModel.Create(new NoopWixStateProvider(), NoopSession.Kibana).ToMsiParams();

		public override Dictionary<string, Guid> ProductCode => ProductGuids.KibanaProductCodes;

		public override Guid UpgradeCode => ProductGuids.KibanaUpgradeCode;

		public override string RegistryKey => @"SOFTWARE\Elastic\Kibana";
	}
}
