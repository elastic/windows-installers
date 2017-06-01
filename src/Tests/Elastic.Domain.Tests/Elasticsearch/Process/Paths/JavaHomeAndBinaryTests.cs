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
		private const string RegistryJdk64 = @"c:\Java\RegistryJdk64";
		private const string RegistryJdk32 = @"c:\Java\RegistryJdk32";
		private const string RegistryJre64 = @"c:\Java\RegistryJre64";
		private const string RegistryJre32 = @"c:\Java\RegistryJre32";

		private static string JavaExe(string folder) => Path.Combine(folder, @"bin\java.exe");

		[Fact] public void StartsInDefaultJavaHomeByDefault() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(DefaultJavaHome));
			});

		[Fact] public void UserHomeTakesPrecedenceOverAll() => JavaChangesOnly(j => j
			.JavaHomeUserVariable(JavaHomeUser)
			.JavaHomeMachineVariable(JavaHomeMachine)
			.JdkRegistry64(RegistryJdk64)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(JavaHomeUser));
			});

		[Fact] public void UserHomeTakesPrecedenceOverRegistry() => JavaChangesOnly(j => j
			.JavaHomeMachineVariable(JavaHomeMachine)
			.JdkRegistry64(RegistryJdk64)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(JavaHomeMachine));
			});

		[Fact] public void UserHomeFallsBackToRegistryScan() => JavaChangesOnly(j => j
			.JdkRegistry64(RegistryJdk64)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(RegistryJdk64));
			});
		
		[Fact] public void Jre64InRegistryIsPickedUp() => JavaChangesOnly(j => j
			.JreRegistry64(RegistryJre64)
			)
			.Start(p =>
			{
				p.ObservableProcess.BinaryCalled.Should().Be(JavaExe(RegistryJre64));
			});

		[Fact] public void NoJavaHomeShouldThrowException() => CreateThrows(s => s
			.Elasticsearch(e=>e.EsHomeMachineVariable(DefaultEsHome))
			.ConsoleSession(ConsoleSession.StartedSession)
			.Java(j=>j)
			, e =>
			{
				e.Message.Should().Contain("no Java installation could be found");
			});

		[Fact] public void JavaHomeSetButExecutableNotFoundThrows() => CreateThrows(s => s
			.Elasticsearch(e=>e.EsHomeMachineVariable(DefaultEsHome))
			.ConsoleSession(ConsoleSession.StartedSession)
			.Java(j=>j.JavaHomeUserVariable(JavaHomeUser))
			.FileSystem(s.AddElasticsearchLibs)
			, e =>
			{
				e.Message.Should().Contain("Java executable not found");
			});
	}
}