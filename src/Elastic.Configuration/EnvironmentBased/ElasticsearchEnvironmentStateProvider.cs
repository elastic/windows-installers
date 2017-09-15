using System;
using System.Reflection;

namespace Elastic.Configuration.EnvironmentBased
{

	public interface IElasticsearchEnvironmentStateProvider
	{
		string RunningExecutableLocation { get; }
		string TempDirectoryVariable { get; }
		
		string HomeDirectoryUserVariable { get; }
		string HomeDirectoryMachineVariable { get; }
		string HomeDirectoryProcessVariable { get; }
		
		string ConfigDirectoryUserVariable { get; }
		string ConfigDirectoryMachineVariable { get; }
		string ConfigDirectoryProcessVariable { get; }
		
		string GetEnvironmentVariable(string variable);

		void SetEsHomeEnvironmentVariable(string esHome);
		void SetEsConfigEnvironmentVariable(string esConfig);
	}

	public class ElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		public static ElasticsearchEnvironmentStateProvider Default { get; } = new ElasticsearchEnvironmentStateProvider();

		public string HomeDirectoryUserVariable => Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.User);
		public string HomeDirectoryMachineVariable => Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Machine);
		public string HomeDirectoryProcessVariable => Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Process);
		public string RunningExecutableLocation => new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

		public string TempDirectoryVariable => Environment.ExpandEnvironmentVariables("%TEMP%");

		public string ConfigDirectoryUserVariable => Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.User);
		public string ConfigDirectoryMachineVariable => Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.Machine);
		public string ConfigDirectoryProcessVariable => Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.Process);

		public string GetEnvironmentVariable(string variable) =>
			Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process)
			?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User)
			?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine);
		
		public void SetEsHomeEnvironmentVariable(string esHome) =>
			Environment.SetEnvironmentVariable("ES_HOME", esHome, EnvironmentVariableTarget.Machine);

		public void SetEsConfigEnvironmentVariable(string esConfig) =>
			Environment.SetEnvironmentVariable("ES_CONFIG", esConfig, EnvironmentVariableTarget.Machine);
	}
}
