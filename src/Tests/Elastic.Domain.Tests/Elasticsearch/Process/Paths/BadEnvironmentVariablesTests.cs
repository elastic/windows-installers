using System;
using System.IO;
using Elastic.ProcessHosts.Process;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;
using kv = System.Collections.Generic.Dictionary<string, string>;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class BadEnvironmentVariablesTests
	{
		private static void ExpectStartupException(string key, string value, Action<StartupException> assert)
		{
			var created = false;
			try
			{
				var elasticsearchProcessTester = new ElasticsearchProcessTester(s => s
					.Elasticsearch(e => e
						.EsHomeMachineVariable(DefaultEsHome)
						.EnvironmentVariables(new kv { { key, value } }))
					.Java(j => j.LegacyJavaHomeMachineVariable(DefaultJavaHome))
					.ConsoleSession(ConsoleSession.StartedSession)
					.FileSystem(fs => s.AddJavaExe(s.AddElasticsearchLibs(fs, null)))
				);
				created = true;
			}
			catch (Exception e)
			{
				var se = e as StartupException;
				if (se == null)
					throw new Exception("Exception was thrown but not of type StartupException see InnerException", e);
				assert(se);
			}
			if (created)
				throw new Exception($"Process tester expected elasticsearch.exe to throw an exception for {key}");
		}

		[Fact]
		public void UserVariableWinsFromMachineVariable()
		{
			var badVariables = new[]
			{
				"ES_CLASSPATH",
				"ES_MIN_MEM",
				"ES_MAX_MEM",
				"ES_HEAP_SIZE",
				"ES_HEAP_NEWSIZE",
				"ES_DIRECT_SIZE",
				"ES_USE_IPV4",
				"ES_GC_OPTS",
				"ES_GC_LOG_FILE"
			};

			foreach (var v in badVariables)
				ExpectStartupException(v, "some value", (e) =>
				{
					e.Message.Should().Contain("The following deprecated environment variables are");
					e.HelpText.Should().NotBeNullOrWhiteSpace();
				});
		}

	}
}