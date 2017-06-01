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

		public string JavaHomeProcessVariable => Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Process);
		public string JavaHomeUserVariable => Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.User);
		public string JavaHomeMachineVariable => Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine);


		public string JdkRegistry64 => RegistrySubKey(Registry64, JdkRootPath, "CurrentVersion");
		public string JdkRegistry32 => RegistrySubKey(Registry32, JdkRootPath, "CurrentVersion");
		public string JreRegistry64 => RegistrySubKey(Registry64, JreRootPath, "CurrentVersion"); 
		public string JreRegistry32 => RegistrySubKey(Registry32, JreRootPath, "CurrentVersion");
		
		private static string RegistrySubKey(RegistryView view, string subKey, string valueOf)
		{
			if (string.IsNullOrWhiteSpace(subKey)) return null;
			var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
			using (var key = baseKey.OpenSubKey(subKey))
				return key?.GetValue(valueOf) as string;
		}
	}
}