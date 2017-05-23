using System;

namespace Elastic.Configuration.EnvironmentBased
{
	public static class KibanaEnvironmentVariables
	{
		public const string KIBANA_HOME_ENV_VAR = "KIBANA_HOME";
		public const string KIBANA_CONFIG_ENV_VAR = "KIBANA_CONFIG";
	}

	public interface IKibanaEnvironmentStateProvider
	{
		string HomeDirectory { get; }
		string ConfigDirectory { get; }

		void SetKibanaHomeEnvironmentVariable(string kibanaHome);
		void SetKibanaConfigEnvironmentVariable(string kibanaConfig);
	}

	public class KibanaEnvironmentStateProvider : IKibanaEnvironmentStateProvider
	{
		public static KibanaEnvironmentStateProvider Default { get; } = new KibanaEnvironmentStateProvider();

		public string HomeDirectory =>
			Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_HOME_ENV_VAR, EnvironmentVariableTarget.Machine)
			?? Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_HOME_ENV_VAR, EnvironmentVariableTarget.User)
			?? Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_HOME_ENV_VAR, EnvironmentVariableTarget.Process);

		public string ConfigDirectory =>
			Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_CONFIG_ENV_VAR, EnvironmentVariableTarget.Machine)
			?? Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_CONFIG_ENV_VAR, EnvironmentVariableTarget.User)
			?? Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_CONFIG_ENV_VAR, EnvironmentVariableTarget.Process);

		public void SetKibanaHomeEnvironmentVariable(string kibanaHome) =>
			Environment.SetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_HOME_ENV_VAR, kibanaHome, EnvironmentVariableTarget.Machine);

		public void SetKibanaConfigEnvironmentVariable(string kibanaConfig) =>
			Environment.SetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_CONFIG_ENV_VAR, kibanaConfig, EnvironmentVariableTarget.Machine);

	}
}