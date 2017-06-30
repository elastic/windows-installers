using System;
using System.Collections.Generic;
using System.Text;
using WindowsInstaller;
using Elastic.Installer.Domain.Configuration.Wix;
using Semver;

namespace Elastic.Installer.Domain.Shared
{
	public class WixStateProvider : IWixStateProvider
	{
		public bool CurrentlyInstalling { get; }
		public SemVersion ExistingVersion { get; }
		public SemVersion CurrentVersion { get; }

		public WixStateProvider(ProductType productType, string currentVersion) 
			: this(productType, currentVersion, currentlyInstalling: false)
		{ }
		
		public WixStateProvider(ProductType productType, string currentVersion, bool currentlyInstalling)
		{
			CurrentlyInstalling = currentlyInstalling;
			string existingVersion;
			var installed = IsAlreadyInstalled(productType, out existingVersion);
			if (!string.IsNullOrEmpty(currentVersion)) 
				CurrentVersion = currentVersion;
			if (installed && !string.IsNullOrEmpty(existingVersion)) 
				ExistingVersion = existingVersion;
		}

		private bool IsAlreadyInstalled(ProductType productType, out string installedVersion)
		{
			var productCodes = GetProductCodes(productType);
			installedVersion = null;
			foreach (var kvp in productCodes)
			{
				var version = kvp.Key;
				var productCode = FormatProductCode(kvp.Value.ToString());
				if (!IsInstalled(productCode)) continue;
				installedVersion = version;
				return true;
			}
			return false;
		}

		private Dictionary<string, Guid> GetProductCodes(ProductType productType)
		{
			switch(productType)
			{
				case ProductType.Elasticsearch: return ProductGuids.ElasticsearchProductCodes;
				case ProductType.Kibana: return ProductGuids.KibanaProductCodes;
				default: throw new ArgumentException($"Unknown product {productType}");
			}
		}

		private bool IsInstalled(string productCode)
		{
			var sb = new StringBuilder(2048);
			uint size = 2048;
			var error = MsiInterop.MsiGetProductInfo(productCode, "InstallDate", sb, ref size);

			if (error == MsiError.UnknownProduct)
				return false;
			if (error == MsiError.NoError)
				return true;
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
