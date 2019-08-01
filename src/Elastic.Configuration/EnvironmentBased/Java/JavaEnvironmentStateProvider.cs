using System;

namespace Elastic.Configuration.EnvironmentBased.Java
{

	public interface IJavaEnvironmentStateProvider
	{
		string JavaHomeUserVariable { get; }
		string JavaHomeMachineVariable { get; }
		string JavaHomeProcessVariable { get; }
	}

	public class JavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private const string JavaHome = "JAVA_HOME";
		public string JavaHomeProcessVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Process);
		public string JavaHomeUserVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.User);
		public string JavaHomeMachineVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Machine);
	}
}