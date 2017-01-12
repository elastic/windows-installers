using System;
using System.Text;
using WindowsInstaller;
using Elastic.Installer.Domain;
using Semver;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using System.Linq;

namespace Elastic.Installer.UI.Shared.Configuration.EnvironmentBased
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
			var productCodes = ProductGuids.ElasticsearchProductCodes
				.Concat(ProductGuids.KibanaProductCodes)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			installedVersion = null;
			foreach (var kvp in productCodes)
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
