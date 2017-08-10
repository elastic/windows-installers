using System.IO;
using System.Linq;
using System.Text;

namespace Elastic.Configuration.EnvironmentBased
{
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

		public string TargetInstallationDirectory => new []
			{
				StateProvider.HomeDirectoryProcessVariable,
				StateProvider.HomeDirectoryUserVariable,
				StateProvider.HomeDirectoryMachineVariable,
			}
			.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));

		public string TargetInstallationConfigDirectory => new []
			{
				StateProvider.ConfigDirectoryProcessVariable,
				StateProvider.ConfigDirectoryUserVariable,
				StateProvider.ConfigDirectoryMachineVariable,
			}
			.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));

		public string HomeDirectory => new []
			{
				StateProvider.HomeDirectoryProcessVariable,
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
					StateProvider.ConfigDirectoryProcessVariable,
					StateProvider.ConfigDirectoryUserVariable,
					StateProvider.ConfigDirectoryMachineVariable,
				}.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));
				if (!string.IsNullOrWhiteSpace(variableOption)) return variableOption;

				var homeDir = this.HomeDirectory;
				return string.IsNullOrEmpty(homeDir) ? null : Path.Combine(homeDir, "config");
			}
		}

		public string GetEnvironmentVariable(string variable) => this.StateProvider.GetEnvironmentVariable(variable);

		public void SetEsHomeEnvironmentVariable(string esHome) => StateProvider.SetEsHomeEnvironmentVariable(esHome);

		public void SetEsConfigEnvironmentVariable(string esConfig) => StateProvider.SetEsConfigEnvironmentVariable(esConfig);
		
		public void UnsetOldConfigVariable() => StateProvider.UnsetOldConfigVariable();
		
		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"ES_HOME (in order of precedence)")
				.AppendLine($"- {nameof(StateProvider.HomeDirectoryProcessVariable)} = {StateProvider.HomeDirectoryProcessVariable}")
				.AppendLine($"- {nameof(StateProvider.HomeDirectoryUserVariable)} = {StateProvider.HomeDirectoryUserVariable}")
				.AppendLine($"- {nameof(StateProvider.HomeDirectoryMachineVariable)} = {StateProvider.HomeDirectoryMachineVariable}")
				.AppendLine($"- From executable location = {HomeDirectoryInferred}")
		
				.AppendLine($"CONF_DIR (in order of precedence)")
				.AppendLine($"- {nameof(StateProvider.ConfigDirectoryProcessVariable)} = {StateProvider.ConfigDirectoryProcessVariable}")
				.AppendLine($"- {nameof(StateProvider.ConfigDirectoryUserVariable)} = {StateProvider.ConfigDirectoryUserVariable}")
				.AppendLine($"- {nameof(StateProvider.ConfigDirectoryMachineVariable)} = {StateProvider.ConfigDirectoryMachineVariable}")
				.AppendLine($"- Fallback to ES_HOME = {Path.Combine(HomeDirectory, "config")}")
				.ToString();
	}
}