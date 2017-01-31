using System;

namespace Elastic.Installer.Domain.Kibana.Configuration.EnvironmentBased
{

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

		private const string KIBANA_HOME_ENV_VAR = "KIBANA_HOME";
		private const string KIBANA_CONFIG_ENV_VAR = "KIBANA_CONFIG";

		public string HomeDirectory =>
			Environment.GetEnvironmentVariable(KIBANA_HOME_ENV_VAR, EnvironmentVariableTarget.Machine)
			?? Environment.GetEnvironmentVariable(KIBANA_HOME_ENV_VAR, EnvironmentVariableTarget.User);

		public string ConfigDirectory =>
			Environment.GetEnvironmentVariable(KIBANA_CONFIG_ENV_VAR, EnvironmentVariableTarget.Machine)
			?? Environment.GetEnvironmentVariable(KIBANA_CONFIG_ENV_VAR, EnvironmentVariableTarget.User);

		public void SetKibanaHomeEnvironmentVariable(string kibanaHome) =>
			Environment.SetEnvironmentVariable(KIBANA_HOME_ENV_VAR, kibanaHome, EnvironmentVariableTarget.Machine);

		public void SetKibanaConfigEnvironmentVariable(string kibanaConfig) =>
			Environment.SetEnvironmentVariable(KIBANA_CONFIG_ENV_VAR, kibanaConfig, EnvironmentVariableTarget.Machine);

	}
}