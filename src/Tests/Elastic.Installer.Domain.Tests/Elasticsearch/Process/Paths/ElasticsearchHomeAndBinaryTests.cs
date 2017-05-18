using System.IO;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class ElasticsearchHomeAndBinaryTests
	{
		private const string JavaHomeUser = @"c:\Java\User";

		private readonly string _executableParentFolder = @"C:\Alternative\Elasticsearch (x86)\weird location";
		private string Executable => Path.Combine(_executableParentFolder, @"bin\elasticsearch.exe");
		private const string EsHomeUser = @"c:\Elasticsearch\User";
		private const string EsHomeMachine = @"c:\Elasticsearch\Machine";

		private static string EsHomeArg(string home) => $"-Des.path.home=\"{home}\"";

		[Fact] public void DefaultEsHomeIsPassedAsArgumentToJava() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsHomeArg(DefaultEsHome));
			});

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

		[Fact] public void UserVariableWinsFromMachineVariable() => ElasticsearchChangesOnly(e=>e
				.EsHomeUserVariable(EsHomeUser)
				.EsHomeMachineVariable(EsHomeMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsHomeArg(EsHomeUser));
			});

		[Fact] public void MachineVariableWinsFromInferredExecutableLocation() => ElasticsearchChangesOnly(e=>e
				.EsHomeMachineVariable(EsHomeMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsHomeArg(EsHomeMachine));
			});

		[Fact] public void DefaultsEsHomeToExecutableParentLocation() => ElasticsearchChangesOnly(e=>e
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsHomeArg(_executableParentFolder));
			});
	}
}