using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public enum MsiError : uint
		{
			NoError = 0,
			NoMoreItems = 259,
			UnknownProduct = 1605,
		}

		[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
		public sealed class MsiInterop
		{
			[DllImport("msi.dll", CharSet = CharSet.Auto)]
			public static extern MsiError MsiGetProductInfo(string product, string property, StringBuilder value, ref uint valueSize);
		}

		public SemVersion ExistingVersion { get; }
		public SemVersion CurrentVersion { get; }

		public NoopWixStateProvider(Product product, string currentVersion)
		{
			if (!string.IsNullOrEmpty(currentVersion))
			{
				string existingVersion;
				var installed = IsAlreadyInstalled(product, out existingVersion);
				this.CurrentVersion = GetVersion(product, currentVersion);
				if (installed) this.ExistingVersion = existingVersion;
			}
		}

		private SemVersion GetVersion(Product product, string currentVersion)
		{
			var current = Guid.Parse(currentVersion);
			var productCodes = GetProductCodes(product);
			return (from productCode in productCodes
				where productCode.Value == current
				select productCode.Key).FirstOrDefault();
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
			switch (product)
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
