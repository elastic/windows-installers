using System;

namespace Elastic.Configuration.EnvironmentBased.Java
{

	public interface IJavaEnvironmentStateProvider
	{
		string EsJavaHomeUserVariable { get; }
		string EsJavaHomeMachineVariable { get; }
		string EsJavaHomeProcessVariable { get; }
		string LegacyJavaHomeUserVariable { get; }
		string LegacyJavaHomeMachineVariable { get; }
		string LegacyJavaHomeProcessVariable { get; }
	}

	public class JavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private const string EsJavaHome = "ES_JAVA_HOME";
		public string EsJavaHomeProcessVariable => Environment.GetEnvironmentVariable(EsJavaHome, EnvironmentVariableTarget.Process)?.Trim();
		public string EsJavaHomeUserVariable => Environment.GetEnvironmentVariable(EsJavaHome, EnvironmentVariableTarget.User)?.Trim();
		public string EsJavaHomeMachineVariable => Environment.GetEnvironmentVariable(EsJavaHome, EnvironmentVariableTarget.Machine)?.Trim();

		private const string JavaHome = "JAVA_HOME";
		public string LegacyJavaHomeProcessVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Process)?.Trim();
		public string LegacyJavaHomeUserVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.User)?.Trim();
		public string LegacyJavaHomeMachineVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Machine)?.Trim();
	}
}