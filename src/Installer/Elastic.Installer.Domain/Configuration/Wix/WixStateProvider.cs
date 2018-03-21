using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32;
using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class WixStateProvider : IWixStateProvider
	{
		private enum MsiError : uint
		{
			NoError = 0,
			InvalidParameter = 87,
			MoreData = 234,
			NoMoreItems = 259,
			UnknownProduct = 1605,
			UnknownProperty = 1608
		}

		[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
		private static class MsiInterop
		{
			/// <summary>
			/// Gets product information for published and installed products.
			/// </summary>
			/// <param name="productCode">the ProductCode</param>
			/// <param name="property">the property of the product to look for</param>
			/// <param name="value">receives the property value</param>
			/// <param name="valueSize">pointer to a variable that specifies the size, in characters, of <paramref name="value"/></param>
			/// <returns></returns>
			[DllImport("msi.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern MsiError MsiGetProductInfo(string productCode, string property, StringBuilder value, ref uint valueSize);

			/// <summary>
			/// Enumerates products with a specified upgrade code. This function lists the currently installed and advertised 
			/// products that have the specified UpgradeCode property in their Property table.
			/// </summary>
			/// <param name="upgradeCode">the UpgradeCode</param>
			/// <param name="reserved">Always set this to 0</param>
			/// <param name="productIndex">Zero-based index of the product within the list of products.</param>
			/// <param name="productCode">receives the ProductCode of the found product</param>
			/// <returns></returns>
			[DllImport("msi.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern MsiError MsiEnumRelatedProducts(string upgradeCode, int reserved, int productIndex, StringBuilder productCode); 
		}

		/// <summary>
		/// The existing version installed
		/// </summary>
		public SemVersion PreviousVersion { get; }

		/// <summary>
		/// The current version from the installer
		/// </summary>
		public SemVersion InstallerVersion { get; }

		public WixStateProvider(Product product, Guid currentProductCode) 
			:this(product, GetVersion(product, currentProductCode))
		{
		}

		public WixStateProvider(Product product, string currentVersion) 
		{
			var installed = IsAlreadyInstalled(product, out var existingVersions);
			this.InstallerVersion = currentVersion;
			// use the latest existing version installed.
			// TODO: What should we do about prerelease versions here?
			if (installed) this.PreviousVersion = existingVersions.OrderByDescending(v => v).First();
		}

		private static string GetVersion(Product product, Guid currentProductCode)
		{
			var productCodes = GetProductCodes(product);
			return (from productCode in productCodes
				where productCode.Value == currentProductCode
				select productCode.Key).First();
		}

		private static bool IsAlreadyInstalled(Product product, out List<SemVersion> installedVersions)
		{
			installedVersions = new List<SemVersion>();
			var upgradeCode = FormatProductCode(GetUpgradeCode(product).ToString());
			var productCodes = GetProductCodes(product);
			var productGuids = productCodes.ToDictionary(d => d.Value, d => d.Key);
			var productCodeBuilder = new StringBuilder(39);
			
			for (var productIndex = 0;; productIndex++)
			{
				var error = MsiInterop.MsiEnumRelatedProducts(upgradeCode, 0, productIndex, productCodeBuilder);

				if (error != MsiError.NoError)
					break;

				var productCode = productCodeBuilder.ToString();
				if (Guid.TryParse(productCode, out var productGuid))
				{
					if (productGuids.TryGetValue(productGuid, out var version))
						installedVersions.Add(version);
					else
					{
						var productVersion = new StringBuilder(1024);
						error = GetVersion(product, productCode, productVersion);
						// TODO: what should we do in the case of any other error value?
						if (error == MsiError.NoError)
							installedVersions.Add(productVersion.ToString());
					}
				}
			}

			return installedVersions.Any();
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

		private static Guid GetUpgradeCode(Product product)
		{
			switch (product)
			{
				case Product.Elasticsearch: return ProductGuids.ElasticsearchUpgradeCode;
				case Product.Kibana: return ProductGuids.KibanaUpgradeCode;
				default: throw new ArgumentException($"Unknown product {product}");
			}
		}

		private static string FormatProductCode(string productCode)
		{
			if (!productCode.StartsWith("{"))
				productCode = "{" + productCode;

			if (!productCode.EndsWith("}"))
				productCode = productCode + "}";

			return productCode.ToUpperInvariant();
		}

		private static MsiError GetVersion(Product product, string productCode, StringBuilder productVersion)
		{
			const string propertyName = "ProductVersion";
			var capacity = productVersion.Capacity;
			var len = (uint)capacity;
			productVersion.Length = 0;
			productCode = FormatProductCode(productCode);
			var error = MsiInterop.MsiGetProductInfo(productCode, propertyName, productVersion, ref len);

			if (error == MsiError.MoreData)
			{
				len++;
				productVersion.EnsureCapacity(capacity);
				error = MsiInterop.MsiGetProductInfo(productCode, propertyName, productVersion, ref len);
			}

			if (error == MsiError.UnknownProduct || error == MsiError.UnknownProperty)
			{
				// try to get version from the registry
				var registryKeyName = new StringBuilder("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Products\\");
				var productGuid = new Guid(productCode);
				var guidBytes = productGuid.ToByteArray();
				for (var i = 0; i < guidBytes.Length; i++)
				{
					var b = guidBytes[i];
					var replacedHex = ((b & 0xf) << 4) + ((b & 0xf0) >> 4);
					registryKeyName.AppendFormat("{0:X2}", replacedHex);
				}
				registryKeyName.Append("\\InstallProperties");

				using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
				using (var key = hklm.OpenSubKey(registryKeyName.ToString()))
				{
					// Parse the version from the DisplayName as opposed to using the DisplayVersion,
					// which does not include the prerelease suffix
					var registryValue = key?.GetValue("DisplayName") as string;
					if (!string.IsNullOrEmpty(registryValue))
					{
						productVersion.Length = 0;
						var version = registryValue.Replace(product.ToString(), string.Empty).Trim();
						productVersion.Append(version);
						error = MsiError.NoError;
					}
				}
			}

			return error;
		}
	}
}
