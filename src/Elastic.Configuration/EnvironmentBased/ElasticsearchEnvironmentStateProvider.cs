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
		
		string NewConfigDirectoryMachineVariable { get; }
		string OldConfigDirectoryMachineVariable { get; }
		string OldConfigDirectoryMachineVariableCopy { get; }

		string GetEnvironmentVariable(string variable);

		void SetEsHomeEnvironmentVariable(string esHome);
		void SetEsConfigEnvironmentVariable(string esConfig);
		void SetOldEsConfigEnvironmentVariable(string esConfig);
		void UnsetOldConfigVariable();
		bool RestoreOldConfigVariable();
	}

	public class ElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		private const string ConfDirOld = "ES_CONFIG";
		private const string ConfDirOldRenamed = "ES_CONFIG_OLD";

		public const string ConfDir = "ES_PATH_CONF";
		public const string EsHome = "ES_HOME";

		public static ElasticsearchEnvironmentStateProvider Default { get; } = new ElasticsearchEnvironmentStateProvider();

		public string HomeDirectoryUserVariable => Environment.GetEnvironmentVariable(EsHome, EnvironmentVariableTarget.User);
		public string HomeDirectoryMachineVariable => Environment.GetEnvironmentVariable(EsHome, EnvironmentVariableTarget.Machine);
		public string HomeDirectoryProcessVariable => Environment.GetEnvironmentVariable(EsHome, EnvironmentVariableTarget.Process);
		public string RunningExecutableLocation => new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

		public string TempDirectoryVariable => Environment.ExpandEnvironmentVariables("%TEMP%");
		
		public string ConfigDirectoryUserVariable => 
			Environment.GetEnvironmentVariable(ConfDir, EnvironmentVariableTarget.User)
			?? Environment.GetEnvironmentVariable(ConfDirOld, EnvironmentVariableTarget.User);

		public string ConfigDirectoryMachineVariable => NewConfigDirectoryMachineVariable ?? OldConfigDirectoryMachineVariable;

		public string NewConfigDirectoryMachineVariable => Environment.GetEnvironmentVariable(ConfDir, EnvironmentVariableTarget.Machine); 
		public string OldConfigDirectoryMachineVariable => Environment.GetEnvironmentVariable(ConfDirOld, EnvironmentVariableTarget.Machine); 
		public string OldConfigDirectoryMachineVariableCopy => Environment.GetEnvironmentVariable(ConfDirOldRenamed, EnvironmentVariableTarget.Machine); 
		
		public string ConfigDirectoryProcessVariable => 
			Environment.GetEnvironmentVariable(ConfDir, EnvironmentVariableTarget.Process)
			?? Environment.GetEnvironmentVariable(ConfDirOld, EnvironmentVariableTarget.Process);

		
		public string GetEnvironmentVariable(string variable) =>
			Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process)
			?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User)
			?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine);

		public void SetEsHomeEnvironmentVariable(string esHome) =>
			Environment.SetEnvironmentVariable(EsHome, esHome, EnvironmentVariableTarget.Machine);

		public void SetEsConfigEnvironmentVariable(string esConfig) =>
			Environment.SetEnvironmentVariable(ConfDir, esConfig, EnvironmentVariableTarget.Machine);
		
		public void SetOldEsConfigEnvironmentVariable(string esConfig) =>
			Environment.SetEnvironmentVariable(ConfDirOld, esConfig, EnvironmentVariableTarget.Machine);
		
		public void UnsetOldConfigVariable()
		{
			var configDirectory = Environment.GetEnvironmentVariable(ConfDirOld, EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(configDirectory)) return;
			
			Environment.SetEnvironmentVariable(ConfDirOldRenamed, configDirectory, EnvironmentVariableTarget.Machine);
			Environment.SetEnvironmentVariable(ConfDirOld, null, EnvironmentVariableTarget.Machine);
		}

		public bool RestoreOldConfigVariable()
		{
			var configDirectory = Environment.GetEnvironmentVariable(ConfDirOldRenamed, EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(configDirectory))
				return false;

			Environment.SetEnvironmentVariable(ConfDirOldRenamed, null, EnvironmentVariableTarget.Machine);
			Environment.SetEnvironmentVariable(ConfDirOld, configDirectory, EnvironmentVariableTarget.Machine);
			return true;
		}
	}
}
