using System;
using System.IO;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class ElasticsearchConfigFolderTests
	{
		private readonly string _executableParentFolder = @"C:\Alternative\Elasticsearch (x86)\weird location";
		private string Executable => Path.Combine(_executableParentFolder, @"bin\elasticsearch.exe");
		private const string EsConfUser = @"c:\Elasticsearch\UserConfig";
		private const string EsConfMachine = @"c:\Elasticsearch\MachineConfig";
		private const string EsConfCommandLine = @"c:\Elasticsearch\ArgCommandLine";

		private static string DefaultEsConf => Path.Combine(DefaultEsHome, "config");

		private static string EsConfArg(string folder) => $"-Epath.conf=\"{folder}\"";

		[Fact] public void DefaultEsHomeIsPassedAsArgumentToJava() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsConfArg(DefaultEsConf));
			});

		[Fact] public void UserVariableWinsFromMachineVariable() => ElasticsearchChangesOnly(e=>e
				.EsConfigUserVariable(EsConfUser)
				.EsConfigMachineVariable(EsConfMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsConfArg(EsConfUser));
			});

		[Fact] public void MachineVariableWinsFromInferredExecutableLocation() => ElasticsearchChangesOnly(e=>e
				.EsConfigMachineVariable(EsConfMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsConfArg(EsConfMachine));
			});

		[Fact] public void DefaultsEsHomeToExecutableParentLocation() => ElasticsearchChangesOnly(e=>e
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				var executableConfig = Path.Combine(_executableParentFolder, "config");
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsConfArg(executableConfig));
			});

		[Fact] public void ArgumentPassedOnCommandLineWins() => ElasticsearchChangesOnly(e=>e
				.ElasticsearchExecutable(Executable)
			, "-E", $"path.conf={EsConfCommandLine}")
			.Start(p =>
			{
				var executableConfig = Path.Combine(_executableParentFolder, "config");
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.NotContain(EsConfArg(executableConfig))
					.And.Contain(EsConfArg(EsConfCommandLine))
					.And.ContainSingle(s=>s.Contains("path.conf"));
			});
		[Fact] public void ArgumentPassedOnCommandLineNeedsFlag() => ElasticsearchChangesOnly(e=>e
				.ElasticsearchExecutable(Executable)
			, $"path.conf={EsConfCommandLine}")
			.Start(p =>
			{
				var executableConfig = Path.Combine(_executableParentFolder, "config");
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.Contain(EsConfArg(executableConfig))
					.And.NotContain(EsConfArg(EsConfCommandLine))
					//the bad flag should still be passed to elasticsearch CLI parser
					//let it do its job informing the user on positional arguments
					.And.Contain($"path.conf={EsConfCommandLine}");
			});

		[Fact] public void ConjoinedArgumentPassedOnCommandLineWins() => ElasticsearchChangesOnly(e=>e
				.ElasticsearchExecutable(Executable)
			, $"-Epath.conf={EsConfCommandLine}")
			.Start(p =>
			{
				var executableConfig = Path.Combine(_executableParentFolder, "config");
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.NotContain(EsConfArg(executableConfig))
					.And.Contain(EsConfArg(EsConfCommandLine))
					.And.ContainSingle(s=>s.Contains("path.conf"));
			});

		[Fact] public void ArgumentPassedOnCommandLineCanContainEqualsSignInPath() => ElasticsearchChangesOnly(e=>e
				.ElasticsearchExecutable(Executable)
			, "-E", $"path.conf={EsConfCommandLine}\\=x==y")
			.Start(p =>
			{
				var executableConfig = Path.Combine(_executableParentFolder, "config");
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty()
					.And.NotContain(EsConfArg(executableConfig))
					.And.Contain(EsConfArg($"{EsConfCommandLine}\\=x==y"))
					.And.ContainSingle(s=>s.Contains("path.conf"));
			});
	}
}