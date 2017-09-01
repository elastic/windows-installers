using System;
using Microsoft.Win32;
using static Microsoft.Win32.RegistryView;

namespace Elastic.Configuration.EnvironmentBased.Java
{

	public interface IJavaEnvironmentStateProvider
	{
		string JavaHomeUserVariable { get; }
		string JavaHomeMachineVariable { get; }
		string JavaHomeProcessVariable { get; }
		
		string JdkRegistry64 { get; }
		string JdkRegistry32 { get; }
		string JreRegistry64  { get; }
		string JreRegistry32 { get; }
	}

	public class JavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private const string JreRootPath = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
		private const string JdkRootPath = "SOFTWARE\\JavaSoft\\Java Development Kit";
		private const string JavaHome = "JAVA_HOME";

		public string JavaHomeProcessVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Process);
		public string JavaHomeUserVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.User);
		public string JavaHomeMachineVariable => Environment.GetEnvironmentVariable(JavaHome, EnvironmentVariableTarget.Machine);


		public string JdkRegistry64 => RegistrySubKey(Registry64, JdkRootPath);
		public string JdkRegistry32 => RegistrySubKey(Registry32, JdkRootPath);
		public string JreRegistry64 => RegistrySubKey(Registry64, JreRootPath); 
		public string JreRegistry32 => RegistrySubKey(Registry32, JreRootPath);
		
		private static string RegistrySubKey(RegistryView view, string subKey)
		{
			if (string.IsNullOrWhiteSpace(subKey)) return null;
			
			var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
			string version = null;
			using (var key = registry.OpenSubKey(subKey))
				version = key?.GetValue("CurrentVersion") as string;
			if (version == null) return null;

			using (var key = registry.OpenSubKey(subKey + "\\" + version))
				return key?.GetValue("JavaHome") as string;
			
		}
	}
}