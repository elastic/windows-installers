using System.Collections.Generic;
using System.Linq;
using System.Net;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install
{
	public class SetupXPackPasswordsTaskTests : InstallationModelTestBase
	{
		[Fact(Skip = "need an integration test for this")]
		void InstallByDefault() => DefaultValidModelForTasks()
			.OnStep(m=>m.XPackModel, step =>
			{
				step.XPackLicense = XPackLicenseMode.Trial;

				step.ElasticUserPassword = "somepass";
				step.KibanaUserPassword = "somepass";
				step.LogstashSystemUserPassword = "somepass";

			})
			.AssertTask(
				(m, s, fs) => new SetupXPackPasswordsTask(m, s, fs), 
				(m, t) =>
				{
				}
			);

		[Theory]
		[InlineData(null, null, "http://localhost:9200/")]
		[InlineData("", null, "http://localhost:9200/")]
		[InlineData(null, 7788, "http://localhost:7788/")]
		[InlineData("", 7788, "http://localhost:7788/")]
		[InlineData("localhost", null, "http://localhost:9200/")]
		[InlineData("localhost", 7788, "http://localhost:7788/")]
		[InlineData("127.0.0.1", null, "http://127.0.0.1:9200/")]
		[InlineData("127.0.0.1", 7788, "http://127.0.0.1:7788/")]
		[InlineData("999.999.999.999", null, "http://999.999.999.999:9200/")]
		[InlineData("999.999.999.999", 7788, "http://999.999.999.999:7788/")]
		[InlineData("test", null, "http://test:9200/")]
		[InlineData("test", 7788, "http://test:7788/")]
		[InlineData("test1, test2", null, "http://test1:9200/")]
		[InlineData("192.168.0.1, test1, test2", 7788, "http://192.168.0.1:7788/")]
		[InlineData("_local_", null, "http://localhost:9200/")]
		[InlineData("_local_", 7788, "http://localhost:7788/")]
		[InlineData("_site_, _local_", null, "http://localhost:9200/")]
		[InlineData("_site_, _local_", 7788, "http://localhost:7788/")]
		[InlineData("_global_, _local_", null, "http://localhost:9200/")]
		[InlineData("_global_, _local_", 7788, "http://localhost:7788/")]
		[InlineData("_local:ipv4_", null, "http://127.0.0.1:9200/")]
		[InlineData("_local:ipv4_", 7788, "http://127.0.0.1:7788/")]
		[InlineData("_local:ipv6_", null, "http://[::1]:9200/")]
		[InlineData("_local:ipv6_", 7788, "http://[::1]:7788/")]
		[InlineData("[\"one.domain.local\", \"two.domain.local\"]", null, "http://localhost:9200/")]
		[InlineData("[\"one.domain.local\", \"two.domain.local\"]", 7788, "http://localhost:7788/")]
		void GetBaseAddressTests(string networkHost, int? httpPort, string expectedBaseAddress)
		{
			SetupXPackPasswordsTask.GetBaseAddress(networkHost, httpPort).Should().Be(expectedBaseAddress);
		}

		[Theory]
		[MemberData(nameof(GetBaseAddressResolveCases))]
		void GetBaseAddressResolveTests(string networkHost, int? httpPort, string expectedBaseAddress)
		{
			SetupXPackPasswordsTask.GetBaseAddress(networkHost, httpPort).Should().Be(expectedBaseAddress);
		}

		public static IEnumerable<object[]> GetBaseAddressResolveCases()
		{
			var hostName = Dns.GetHostName();
			var nonLoopbackAddresses = Dns.GetHostAddresses(hostName).Where(a => !IPAddress.IsLoopback(a)
				&& (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6));

			var nonLoopback = nonLoopbackAddresses.FirstOrDefault();
			if (nonLoopback != null)
			{
				var address = nonLoopback.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? $"[{nonLoopback}]" : nonLoopback.ToString();
				yield return new object[] {"_site_", null, $"http://{address}:9200/"};
				yield return new object[] {"_global_", null, $"http://{address}:9200/"};
				yield return new object[] {"_site_", 7788, $"http://{address}:7788/"};
				yield return new object[] {"_global_", 7788, $"http://{address}:7788/"};
			}
			var nonLoopbackV4 = nonLoopbackAddresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
			if (nonLoopbackV4 != null)
			{
				var address = nonLoopbackV4.ToString();
				yield return new object[] {"_site:ipv4_", null, $"http://{address}:9200/"};
				yield return new object[] {"_global:ipv4_", null, $"http://{address}:9200/"};
				yield return new object[] {"_site:ipv4_", 7788, $"http://{address}:7788/"};
				yield return new object[] {"_global:ipv4_", 7788, $"http://{address}:7788/"};
			}
			var nonLoopbackV6 = nonLoopbackAddresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
			if (nonLoopbackV6 != null)
			{
				var address = $"[{nonLoopbackV6}]";
				yield return new object[] {"_site:ipv6_", null, $"http://{address}:9200/"};
				yield return new object[] {"_global:ipv6_", null, $"http://{address}:9200/"};
				yield return new object[] {"_site:ipv6_", 7788, $"http://{address}:7788/"};
				yield return new object[] {"_global:ipv6_", 7788, $"http://{address}:7788/"};
			}
		}
	}
}
