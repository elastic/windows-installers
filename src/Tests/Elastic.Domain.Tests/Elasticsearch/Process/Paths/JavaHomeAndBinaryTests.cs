using System.IO;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class LegacyJavaHomeAndBinaryTests
	{
		private const string JavaHomeUser = @"c:\LegacyJavaHome\User";
		private const string JavaHomeMachine = @"c:\LegacyJavaHome\Machine";
		private const string RegistryJdk64 = @"c:\LegacyJavaHome\RegistryJdk64";
		private const string RegistryJdk32 = @"c:\LegacyJavaHome\RegistryJdk32";
		private const string RegistryJre64 = @"c:\LegacyJavaHome\RegistryJre64";
		private const string RegistryJre32 = @"c:\LegacyJavaHome\RegistryJre32";

		private static string JavaExe(string folder) => Path.Combine(folder, @"bin\java.exe");

		[Fact]
		public void StartsInDefaultJavaHomeByDefault() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(DefaultJavaHome));
			});

		[Fact]
		public void JavaHomeUsesBundledJdk() => JavaChangesOnly(s => s)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(Path.Combine(DefaultEsHome, "jdk")));
			});

		[Fact]
		public void JavaHomeSetButExecutableNotFoundThrows() => CreateThrows(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.ConsoleSession(ConsoleSession.StartedSession)
			.Java(j => j.LegacyJavaHomeUserVariable(JavaHomeUser))
			.FileSystem(s.AddElasticsearchLibs),
			e =>
			{
				e.Message.Should().Contain("java.exe does not exist at");
			});
	}

	public class EsJavaHomeAndBinaryTests
	{
		private const string JavaHomeUser = @"c:\EsJavaHome\User";
		private const string JavaHomeMachine = @"c:\EsJavaHome\Machine";
		private const string RegistryJdk64 = @"c:\EsJavaHome\RegistryJdk64";
		private const string RegistryJdk32 = @"c:\EsJavaHome\RegistryJdk32";
		private const string RegistryJre64 = @"c:\EsJavaHome\RegistryJre64";
		private const string RegistryJre32 = @"c:\EsJavaHome\RegistryJre32";

		private static string JavaExe(string folder) => Path.Combine(folder, @"bin\java.exe");

		[Fact]
		public void StartsInDefaultJavaHomeByDefault() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(DefaultJavaHome));
			});

		[Fact]
		public void UserHomeTakesPrecedenceOverAll() => JavaChangesOnly(j => j
			.EsJavaHomeUserVariable(JavaHomeUser)
			.EsJavaHomeMachineVariable(JavaHomeMachine)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(JavaHomeUser));
			});

		[Fact]
		public void JavaHomeUsesBundledJdk() => JavaChangesOnly(s => s)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(Path.Combine(DefaultEsHome, "jdk")));
			});

		[Fact]
		public void JavaHomeSetButExecutableNotFoundThrows() => CreateThrows(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.ConsoleSession(ConsoleSession.StartedSession)
			.Java(j => j.EsJavaHomeUserVariable(JavaHomeUser))
			.FileSystem(s.AddElasticsearchLibs),
			e =>
			{
				e.Message.Should().Contain("java.exe does not exist at");
			});
	}
}