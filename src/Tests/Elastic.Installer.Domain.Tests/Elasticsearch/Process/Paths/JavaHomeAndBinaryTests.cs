using System.IO;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class JavaHomeAndBinaryTests
	{
		private const string JavaHomeUser = @"c:\Java\User";
		private const string JavaHomeMachine = @"c:\Java\Machine";
		private const string JavaHomeRegistry = @"c:\Java\Registry";

		private static string JavaExe(string folder) => Path.Combine(folder, @"bin\java.exe");

		[Fact] public void StartsInDefaultJavaHomeByDefault() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(DefaultJavaHome));
			});

		[Fact] public void UserHomeTakesPrecedenceOverAll() => JavaChangesOnly(j => j
			.JavaHomeCurrentUser(JavaHomeUser)
			.JavaHomeMachine(JavaHomeMachine)
			.JavaHomeRegistry(JavaHomeRegistry)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(JavaHomeUser));
			});

		[Fact] public void UserHomeTakesPrecedenceOverRegistry() => JavaChangesOnly(j => j
			.JavaHomeMachine(JavaHomeMachine)
			.JavaHomeRegistry(JavaHomeRegistry)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(JavaHomeMachine));
			});

		[Fact] public void UserHomeFallsBackToRegistryScan() => JavaChangesOnly(j => j
			.JavaHomeRegistry(JavaHomeRegistry)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(JavaHomeRegistry));
			});

		[Fact] public void NoJavaHomeShouldThrowException() => CreateThrows(s => s
			.Elasticsearch(e=>e.EsHomeMachineVariable(DefaultEsHome))
			.ConsoleSession(ConsoleSession.Valid)
			.Java(j=>j)
			, e =>
			{
				e.Message.Should().Contain("no Java installation could be found");
			});

		[Fact] public void JavaHomeSetButExecutableNotFoundThrows() => CreateThrows(s => s
			.Elasticsearch(e=>e.EsHomeMachineVariable(DefaultEsHome))
			.ConsoleSession(ConsoleSession.Valid)
			.Java(j=>j.JavaHomeCurrentUser(JavaHomeUser))
			.FileSystem(s.AddElasticsearchLibs)
			, e =>
			{
				e.Message.Should().Contain("Java executable not found");
			});
	}
}