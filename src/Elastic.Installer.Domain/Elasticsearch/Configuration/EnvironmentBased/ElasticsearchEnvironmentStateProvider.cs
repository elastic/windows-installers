using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased
{

	public interface IElasticsearchEnvironmentStateProvider
	{
		string HomeDirectoryUserVariable { get; }
		string HomeDirectoryMachineVariable { get; }
		string RunningExecutableLocation { get; }

		string ConfigDirectoryUserVariable { get; }
		string ConfigDirectoryMachineVariable { get; }


		void SetEsHomeEnvironmentVariable(string esHome);
		void SetEsConfigEnvironmentVariable(string esConfig);
	}

	public class ElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		public static ElasticsearchEnvironmentStateProvider Default { get; } = new ElasticsearchEnvironmentStateProvider();

		public string HomeDirectoryUserVariable => Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.User);
		public string HomeDirectoryMachineVariable => Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Machine);
		public string RunningExecutableLocation => new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

		public string ConfigDirectoryUserVariable => Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.User);
		public string ConfigDirectoryMachineVariable => Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.Machine);


		public void SetEsHomeEnvironmentVariable(string esHome) =>
			Environment.SetEnvironmentVariable("ES_HOME", esHome, EnvironmentVariableTarget.Machine);

		public void SetEsConfigEnvironmentVariable(string esConfig) =>
			Environment.SetEnvironmentVariable("ES_CONFIG", esConfig, EnvironmentVariableTarget.Machine);
	}

	public class ElasticsearchEnvironmentConfiguration
	{
		public static ElasticsearchEnvironmentConfiguration Default { get; } = new ElasticsearchEnvironmentConfiguration(new ElasticsearchEnvironmentStateProvider());

		public IElasticsearchEnvironmentStateProvider StateProvider { get; }

		public ElasticsearchEnvironmentConfiguration(IElasticsearchEnvironmentStateProvider stateProvider)
		{
			StateProvider = stateProvider ?? new ElasticsearchEnvironmentStateProvider();
		}

		private string HomeDirectoryInferred
		{
			get
			{
				var codeBase = StateProvider.RunningExecutableLocation;
				if (string.IsNullOrWhiteSpace(codeBase)) return null;
				var codeBaseFolder = new DirectoryInfo(Path.GetDirectoryName(codeBase));
				var directoryInfo = codeBaseFolder.Parent;
				return directoryInfo?.FullName;
			}
		}

		public string HomeDirectory => new []
			{
				StateProvider.HomeDirectoryUserVariable,
				StateProvider.HomeDirectoryMachineVariable,
				this.HomeDirectoryInferred
			}
			.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));

		public string ConfigDirectory
		{
			get
			{
				var variableOption = new []
				{
					StateProvider.ConfigDirectoryUserVariable,
					StateProvider.ConfigDirectoryMachineVariable,
				}.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));
				if (!string.IsNullOrWhiteSpace(variableOption)) return variableOption;

				var homeDir = this.HomeDirectory;
				return string.IsNullOrEmpty(homeDir) ? null : Path.Combine(homeDir, "config");
			}
        }

		public void SetEsHomeEnvironmentVariable(string esHome) => StateProvider.SetEsHomeEnvironmentVariable(esHome);

		public void SetEsConfigEnvironmentVariable(string esConfig) => StateProvider.SetEsConfigEnvironmentVariable(esConfig);
	}

}