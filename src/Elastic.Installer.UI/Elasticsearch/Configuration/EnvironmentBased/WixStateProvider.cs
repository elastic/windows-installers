using System;
using System.Text;
using WindowsInstaller;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Semver;

namespace Elastic.Installer.UI.Elasticsearch.Configuration.EnvironmentBased
{
	public class WixStateProvider : IWixStateProvider
	{
		public SemVersion ExistingVersion { get; }
		public SemVersion CurrentVersion { get; }

		public WixStateProvider(string currentVersion)
		{
			string existingVersion;
			var installed = IsAlreadyInstalled(out existingVersion);
			this.CurrentVersion = currentVersion;
			if (installed) this.ExistingVersion = existingVersion;
		}

		private bool IsAlreadyInstalled(out string installedVersion)
		{
			installedVersion = null;
			foreach (var kvp in ProductGuids.ElasticsearchProductCodes)
			{
				var version = kvp.Key;
				var productCode = FormatProductCode(kvp.Value.ToString());
				if (IsInstalled(productCode))
				{
					installedVersion = version;
					return true;
				}
			}
			return false;
		}

		private bool IsInstalled(string productCode)
		{
			var sb = new StringBuilder(2048);
			uint size = 2048;
			var error = MsiInterop.MsiGetProductInfo(productCode, "InstallDate", sb, ref size);

			if (error == MsiError.UnknownProduct)
				return false;
			else if (error == MsiError.NoError)
				return true;
			else
				throw new Exception(error.ToString());
		}

		private string FormatProductCode(string productCode)
		{
			if (!productCode.StartsWith("{"))
				productCode = "{" + productCode;

			if (!productCode.EndsWith("}"))
				productCode = productCode + "}";

			return productCode.ToUpperInvariant();
		}
	}
}
