using System;
using System.Text;
using WindowsInstaller;
using Elastic.Installer.Domain;
using Semver;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using System.Linq;
using System.Collections.Generic;

namespace Elastic.Installer.UI.Shared.Configuration.EnvironmentBased
{
	public class WixStateProvider : IWixStateProvider
	{
		public SemVersion ExistingVersion { get; }
		public SemVersion CurrentVersion { get; }

		public WixStateProvider(Product product, string currentVersion)
		{
			string existingVersion;
			var installed = IsAlreadyInstalled(product, out existingVersion);
			this.CurrentVersion = currentVersion;
			if (installed) this.ExistingVersion = existingVersion;
		}

		private bool IsAlreadyInstalled(Product product, out string installedVersion)
		{
			var productCodes = GetProductCodes(product);
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

		private Dictionary<string, Guid> GetProductCodes(Product product)
		{
			switch(product)
			{
				case Product.Elasticsearch: return ProductGuids.ElasticsearchProductCodes;
				case Product.Kibana: return ProductGuids.KibanaProductCodes;
				default: throw new ArgumentException($"Unknown product {product}");
			}
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
