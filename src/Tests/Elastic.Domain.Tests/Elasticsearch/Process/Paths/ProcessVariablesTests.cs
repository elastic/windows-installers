using System;
using System.IO;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Elastic.ProcessHosts.Process;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;
using kv = System.Collections.Generic.Dictionary<string, string>;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class ProcessVariablesTests
	{
		private static kv Start(string var, string value) => Start(e=>e.EnvironmentVariables(new kv {{var, value}}));

		private static kv Start(Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> setter = null)
		{
			setter = setter ?? (e => e);
			var elasticsearchProcessTester = new ElasticsearchProcessTester(s => s
				.Elasticsearch(e => setter(e.EsHomeMachineVariable(DefaultEsHome)))
				.Java(j => j.JavaHomeMachineVariable(DefaultJavaHome))
				.ConsoleSession(ConsoleSession.StartedSession)
				.FileSystem(fs => s.AddJavaExe(s.AddElasticsearchLibs(fs, null)))
			);
			elasticsearchProcessTester.Process.Start();
			return elasticsearchProcessTester.ObservableProcess.ProcessVariables;
		}
		[Fact] public void HostNameIsNotNull()
		{
			var processVariables = Start();
			processVariables.Should().ContainKey("HOSTNAME");
			processVariables["HOSTNAME"].Should().NotBeNullOrWhiteSpace();
		}
		[Fact] public void EsTmpDirIsSetWhenNotDefined()
		{
			var processVariables = Start();
			processVariables.Should().ContainKey("ES_TMPDIR");
			processVariables["ES_TMPDIR"].Should().NotBeNullOrWhiteSpace();
			processVariables["ES_TMPDIR"].Should().Be(Path.Combine(DefaultTempDirectory, "elasticsearch"));
		}
		[Fact] public void EsTmpDirIsNotSetWhenAlreadyDefined()
		{
			var processVariables = Start("ES_TMPDIR", "value");
			processVariables.Should().ContainKey("ES_TMPDIR");
			processVariables["ES_TMPDIR"].Should().EndWith(@"\value");
		}

		[Fact] public void JavaOptsDoesNotMakeItToProcessVariables()
		{
			var processVariables = Start("JAVA_OPTS", "x");
			processVariables.Should().ContainKey("JAVA_OPTS");
			processVariables["JAVA_OPTS"].Should().BeNull();
		}
		[Fact] public void JavaToolOptionsDoesNotMakeItToProcessVariables()
		{
			var processVariables = Start("JAVA_TOOL_OPTIONS", "x");
			processVariables.Should().ContainKey("JAVA_TOOL_OPTIONS");
			processVariables["JAVA_TOOL_OPTIONS"].Should().BeNull();
		}
	}
}