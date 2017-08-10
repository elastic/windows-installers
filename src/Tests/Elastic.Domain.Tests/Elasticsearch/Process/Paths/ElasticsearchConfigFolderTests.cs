using System;
using System.IO;
using Elastic.ProcessHosts.Process;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Paths
{
	public class ElasticsearchConfigFolderTests
	{
		private readonly string _executableParentFolder = @"C:\Alternative\Elasticsearch (x86)\weird location";
		private string Executable => Path.Combine(_executableParentFolder, @"bin\elasticsearch.exe");
		private const string EsConfProcess = @"c:\Elasticsearch\Process";
		private const string EsConfUser = @"c:\Elasticsearch\UserConfig";
		private const string EsConfMachine = @"c:\Elasticsearch\MachineConfig";
		private const string EsConfCommandLine = @"c:\Elasticsearch\ArgCommandLine";

		private static string DefaultEsConf => Path.Combine(DefaultEsHome, "config");

		private static string EsConfArg(string folder) => $"-Des.path.conf=\"{folder}\"";

		[Fact] public void DefaultEsHomeIsPassedAsArgumentToJava() => AllDefaults()
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsConfArg(DefaultEsConf));
			});

		[Fact] public void ProcessVariableWinsFromUserVariable() => ElasticsearchChangesOnly(e=>e
				.EsConfigProcessVariable(EsConfProcess)
				.EsConfigUserVariable(EsConfUser)
				.EsConfigMachineVariable(EsConfMachine)
				.ElasticsearchExecutable(Executable)
			)
			.Start(p =>
			{
				p.ObservableProcess.ArgsCalled.Should().NotBeNullOrEmpty().And.Contain(EsConfArg(EsConfProcess));
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

		[Fact] public void ArgumentPassedOnCommandLineWins() =>
			InstantiateThrows(() =>
				ElasticsearchChangesOnly(e => e
					.ElasticsearchExecutable(Executable)
					, "-E", $"path.conf={EsConfCommandLine}"
				)
			);
		
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

		[Fact] public void ConjoinedArgumentPassedOnCommandLineWins() =>
			InstantiateThrows(() =>
				ElasticsearchChangesOnly(e => e
						.ElasticsearchExecutable(Executable)
					, $"-Epath.conf={EsConfCommandLine}"
				)
			);
			
			
			

		[Fact] public void ArgumentPassedOnCommandLineCanContainEqualsSignInPath() =>
			InstantiateThrows(() => 
				ElasticsearchChangesOnly(e => e
					.ElasticsearchExecutable(Executable)
					, "-E", $"path.conf={EsConfCommandLine}\\=x==y"
				)
			);
			
		private static void InstantiateThrows(Action instantiate) => instantiate
			.ShouldThrowExactly<StartupException>()
			.WithMessage("setting -E path.conf is no longer supported");
		
	}
}