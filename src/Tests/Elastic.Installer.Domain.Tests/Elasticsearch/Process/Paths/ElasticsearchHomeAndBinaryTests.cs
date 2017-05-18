using System.IO;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class ElasticsearchHomeAndBinaryTests
	{
		private const string JavaHomeUser = @"c:\Java\User";

		[Fact] public void MissingElasticsearchLibFolderThrows() => CreateThrows(s => s
				.Elasticsearch(e=>e.EsHomeMachineVariable(DefaultEsHome))
				.ConsoleSession(ConsoleSession.Valid)
				.Java(j=>j.JavaHomeCurrentUser(JavaHomeUser))
				.FileSystem(s.AddJavaExe)
			, e =>
			{
				e.Message.Should().Contain("Expected a 'lib' directory inside:");
			});

		[Fact] public void MissingElasticsearchJarThrows() => CreateThrows(s => s
				.Elasticsearch(e=>e.EsHomeMachineVariable(DefaultEsHome))
				.ConsoleSession(ConsoleSession.Valid)
				.Java(j=>j.JavaHomeCurrentUser(JavaHomeUser))
				.FileSystem(fs => {
					fs = s.AddJavaExe(fs);
					fs.AddDirectory(Path.Combine(DefaultEsHome, "lib"));
					return fs;
				})
			, e =>
			{
				e.Message.Should().Contain("No elasticsearch jar found in:");
			});
	}
}