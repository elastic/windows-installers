using System;
using Microsoft.Win32;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased
{

	public interface IJavaEnvironmentStateProvider
	{
		string JavaHomeCurrentUser { get; }
		string JavaHomeMachine { get; }
		string JavaHomeRegistry  { get; }

		void SetJavaHomeEnvironmentVariable(string javaHome);
	}

	public class JavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private const string JreRootPath = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
		private const string JdkRootPath = "SOFTWARE\\JavaSoft\\Java Development Kit";

		public string JavaHomeCurrentUser => Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.User);
		public string JavaHomeMachine => Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine);
		public string JavaHomeRegistry
		{
			get
			{
				var path = JvmRegistrySubKey + "\\" + JvmRegistryVersion;
				return ReadRegistry(path, "JavaHome");
			}
		}
		public void SetJavaHomeEnvironmentVariable(string javaHome) => 
			Environment.SetEnvironmentVariable("JAVA_HOME", javaHome, EnvironmentVariableTarget.Machine);

		private static string JvmRegistrySubKey =>
			RegistryExists(JdkRootPath) ? JdkRootPath
			: RegistryExists(JreRootPath) ? JreRootPath
			: null; 
			
		private static string JvmRegistryVersion => ReadRegistry(JvmRegistrySubKey, "CurrentVersion");

		private static bool RegistryExists(string subKey) =>
			RegistryExists(RegistryView.Registry64, subKey) || RegistryExists(RegistryView.Registry32, subKey);

		private static bool RegistryExists(RegistryView view, string subKey)
		{
			if (string.IsNullOrWhiteSpace(subKey)) return false;
			var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
			using (var key = baseKey.OpenSubKey(subKey))
				return key != null;
		}

		private static string ReadRegistry(string subKey, string valueOf)
		{
			if (string.IsNullOrWhiteSpace(subKey)) return null;
			var x64 = RegistrySubKey(RegistryView.Registry64, subKey, valueOf);
			if (!string.IsNullOrEmpty(x64)) return x64;
			var x86 = RegistrySubKey(RegistryView.Registry32, subKey, valueOf);
			return x86;
		}

		private static string RegistrySubKey(RegistryView view, string subKey, string valueOf)
		{
			if (string.IsNullOrWhiteSpace(subKey)) return null;
			var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
			using (var key = baseKey.OpenSubKey(subKey))
				return key?.GetValue(valueOf) as string;
		}
	}
}