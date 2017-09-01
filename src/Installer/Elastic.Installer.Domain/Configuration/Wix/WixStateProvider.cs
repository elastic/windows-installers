using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class WixStateProvider : IWixStateProvider
	{
		private enum MsiError : uint
		{
			NoError = 0,
			NoMoreItems = 259,
			UnknownProduct = 1605,
		}

		[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
		private static class MsiInterop
		{
			[DllImport("msi.dll", CharSet = CharSet.Auto)]
			public static extern MsiError MsiGetProductInfo(string product, string property, StringBuilder value, ref uint valueSize);
		}

		public SemVersion ExistingVersion { get; }
		public SemVersion CurrentVersion { get; }

		public WixStateProvider(Product product, Guid currentProductCode) 
			:this(product, GetVersion(product, currentProductCode))
		{
		}

		public WixStateProvider(Product product, string currentVersion) 
		{
			string existingVersion;
			var installed = IsAlreadyInstalled(product, out existingVersion);
			this.CurrentVersion = currentVersion;
			if (installed) this.ExistingVersion = existingVersion;
		}

		private static string GetVersion(Product product, Guid currentProductCode)
		{
			var productCodes = GetProductCodes(product);
			return (from productCode in productCodes
				where productCode.Value == currentProductCode
				select productCode.Key).FirstOrDefault();
		}

		private static bool IsAlreadyInstalled(Product product, out string installedVersion)
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

		private static Dictionary<string, Guid> GetProductCodes(Product product)
		{
			switch (product)
			{
				case Product.Elasticsearch: return ProductGuids.ElasticsearchProductCodes;
				case Product.Kibana: return ProductGuids.KibanaProductCodes;
				default: throw new ArgumentException($"Unknown product {product}");
			}
		}

		private static bool IsInstalled(string productCode)
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

		private static string FormatProductCode(string productCode)
		{
			if (!productCode.StartsWith("{"))
				productCode = "{" + productCode;

			if (!productCode.EndsWith("}"))
				productCode = productCode + "}";

			return productCode.ToUpperInvariant();
		}
	}
}
