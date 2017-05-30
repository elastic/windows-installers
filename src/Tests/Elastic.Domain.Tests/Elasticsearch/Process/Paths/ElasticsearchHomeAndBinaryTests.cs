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
		private const string EsHomeProcess = @"c:\Elasticsearch\Process";
		private const string EsHomeUser = @"c:\Elasticsearch\User";
		private const string EsHomeMachine = @"c:\Elasticsearch\Machine";
		private const string EsHomeCommandLine = @"c:\Elasticsearch\CommandLine";

		private static string EsHomeArg(string home) => $"-Des.path.home=\"{home}\"";

		[Fact] public void DefaultEsHomeIsPassedAsArgumentToJava() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.Contain(EsHomeArg(DefaultEsHome));
			});

		[Fact] public void MissingElasticsearchLibFolderThrows() => CreateThrows(s => s
				.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
				.ConsoleSession(ConsoleSession.StartedSession)
				.Java(j => j.JavaHomeCurrentUser(JavaHomeUser))
				.FileSystem(s.AddJavaExe)
			, e => { e.Message.Should().Contain("Expected a 'lib' directory inside:"); });

		[Fact] public void MissingElasticsearchJarThrows() => CreateThrows(s => s
				.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
				.ConsoleSession(ConsoleSession.StartedSession)
				.Java(j => j.JavaHomeCurrentUser(JavaHomeUser))
				.FileSystem(fs =>
				{
					fs = s.AddJavaExe(fs);
					fs.AddDirectory(Path.Combine(DefaultEsHome, "lib"));
					return fs;
				})
			, e => { e.Message.Should().Contain("No elasticsearch jar found in:"); });

		
		[Fact] public void ProcessVariableWinsFromUserVariable() => ElasticsearchChangesOnly(e => e
				.EsHomeProcessVariable(EsHomeProcess)
				.EsHomeUserVariable(EsHomeUser)
				.EsHomeMachineVariable(EsHomeMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.Contain(EsHomeArg(EsHomeProcess));
			});
		[Fact] public void UserVariableWinsFromMachineVariable() => ElasticsearchChangesOnly(e => e
				.EsHomeUserVariable(EsHomeUser)
				.EsHomeMachineVariable(EsHomeMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.Contain(EsHomeArg(EsHomeUser));
			});

		[Fact] public void MachineVariableWinsFromInferredExecutableLocation() => ElasticsearchChangesOnly(e => e
				.EsHomeMachineVariable(EsHomeMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.Contain(EsHomeArg(EsHomeMachine));
			});

		[Fact] public void DefaultsEsHomeToExecutableParentLocation() => ElasticsearchChangesOnly(e => e
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.Contain(EsHomeArg(_executableParentFolder));
			});

		[Fact] public void ArgumentPassedOnCommandLineWins() =>
			ElasticsearchChangesOnly(
				EsHomeCommandLine,
				e => e.ElasticsearchExecutable(Executable)
				, "-E", $"path.home={EsHomeCommandLine}"
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should()
					.NotBeNullOrEmpty()
					.And.NotContain(EsHomeArg(_executableParentFolder))
					.And.Contain(EsHomeArg(EsHomeCommandLine))
					.And.ContainSingle(s => s.Contains("path.home"));
			});

		[Fact] public void ArgumentPassedOnCommandLineNeedsFlag() =>
			ElasticsearchChangesOnly(
				_executableParentFolder,
				e => e.ElasticsearchExecutable(Executable)
				, $"path.home={EsHomeCommandLine}"
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should()
					.NotBeNullOrEmpty()
					.And.Contain(EsHomeArg(_executableParentFolder))
					.And.NotContain(EsHomeArg(EsHomeCommandLine))
					//the bad flag should still be passed to elasticsearch CLI parser
					//let it do its job informing the user on positional arguments
					.And.Contain($"path.home={EsHomeCommandLine}");
			});

		[Fact] public void ConjoinedArgumentPassedOnCommandLineWins() =>
			ElasticsearchChangesOnly(
				EsHomeCommandLine,
				e => e.ElasticsearchExecutable(Executable)
				, $"-Epath.home={EsHomeCommandLine}"
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should()
					.NotBeNullOrEmpty()
					.And.NotContain(EsHomeArg(_executableParentFolder))
					.And.Contain(EsHomeArg(EsHomeCommandLine))
					.And.ContainSingle(s => s.Contains("path.home"));
			});

		[Fact]
		public void ArgumentPassedOnCommandLineCanContainEqualsSignInPath()
		{
			var home = $"{EsHomeCommandLine}\\=x==y";
			ElasticsearchChangesOnly(
				home, e => e.ElasticsearchExecutable(Executable), "-E", $"path.home={home}"
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should()
					.NotBeNullOrEmpty()
					.And.NotContain(EsHomeArg(_executableParentFolder))
					.And.Contain(EsHomeArg(home))
					.And.ContainSingle(s => s.Contains("path.home"));
			});
		}
	}
}