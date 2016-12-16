using System;
using System.IO;
using System.Text;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Model;
using ReactiveUI;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Locations
{
	public class LocationsModel : StepBase<LocationsModel, LocationsModelValidator>
	{
		private const string ProgramDataEnvironmentVariable = "ALLUSERSPROFILE";
		private const string Logs = "logs";
		private const string Data = "data";
		private const string Config = "config";
		private const string DefaultWritableDirectoryArgument = 
			@"[%" + ProgramDataEnvironmentVariable + @"]\" + CompanyFolderName + @"\" + ProductFolderName;
		private const string DefaultLogsDirectoryArgument = DefaultWritableDirectoryArgument + @"\" + Logs;
		private const string DefaultDataDirectoryArgument = DefaultWritableDirectoryArgument + @"\" + Data;
		private const string DefaultConfigDirectoryArgument = DefaultWritableDirectoryArgument + @"\" + Config;
		public const string CompanyFolderName = "Elastic";
		public const string ProductFolderName = "Elasticsearch";

		public static string DefaultProgramFiles => 
			Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

		public static readonly string DefaultCompanyDirectory =
			Path.Combine(Environment.GetEnvironmentVariable(ProgramDataEnvironmentVariable), CompanyFolderName);

		public static readonly string DefaultWritableDirectory = Path.Combine(DefaultCompanyDirectory, ProductFolderName);

		public static readonly string DefaultInstallationDirectory = Path.Combine(DefaultProgramFiles, CompanyFolderName, ProductFolderName);
		public static readonly string DefaultMsiLogFileLocation = Path.Combine(DefaultWritableDirectory, "install.log");
		public static readonly string DefaultLogsDirectory = Path.Combine(DefaultWritableDirectory, Logs);
		public static readonly string DefaultDataDirectory = Path.Combine(DefaultWritableDirectory, Data);
		public static readonly string DefaultConfigDirectory = Path.Combine(DefaultWritableDirectory, Config);

		private bool _refreshing;
		private readonly IElasticsearchEnvironmentStateProvider _environmentStateProvider;
		private readonly ElasticsearchYamlConfiguration _yamlConfiguration;

		public LocationsModel(
			IElasticsearchEnvironmentStateProvider environmentStateProvider, 
			ElasticsearchYamlConfiguration yamlConfiguration, 
			VersionConfiguration versionConfig)
		{
			this.IsRelevant = !versionConfig.AlreadyInstalled;
			this.Header = "Locations";
			this._environmentStateProvider = environmentStateProvider;
			this._yamlConfiguration = yamlConfiguration;

			this.Refresh();
			this._refreshing = true;

			//when configurelocations is checked and place paths in samefolder is not set, set configureall locations to true
			var prop = this.WhenAny(
				vm => vm.ConfigureLocations,
				vm => vm.PlaceWritableLocationsInSamePath,
				vm => vm.InstallDir,
				(configureLocations, samePath, i) => configureLocations.GetValue() && !samePath.GetValue()
				)
				.ToProperty(this, vm => vm.ConfigureAllLocations, out configureAllLocations);

			this.WhenAny(
				vm => vm.LogsDirectory,
				(c) => {
					var v = c.GetValue();
					if (Path.IsPathRooted(v))
						return Path.Combine(c.GetValue(), "elasticsearch.log");
					return null;
				})
				.ToProperty(this, vm => vm.ElasticsearchLog, out elasticsearchLog);

			this.ThrownExceptions.Subscribe(e =>
			{
			});

			//If install, config, logs or data dir are set force ConfigureLocations to true
			this.WhenAny(
				vm => vm.InstallDir,
				vm => vm.ConfigDirectory,
				vm => vm.LogsDirectory,
				vm => vm.DataDirectory,
				(i, c, l, d) => !this._refreshing
				)
				.Subscribe(x => { if (x) this.ConfigureLocations = true; });

			this.WhenAny(
				vm => vm.ConfigureLocations,
				(c) => !this._refreshing && !c.Value
				)
				.Subscribe(x => { if (x) this.Refresh(); });

			this._refreshing = false;
		}

		public sealed override void Refresh()
		{
			this._refreshing = true;
			this.ConfigureLocations = false;
			this.PlaceWritableLocationsInSamePath = false;
			this.SetDefaultLocations();
			this._refreshing = false;
		}

		public void SetDefaultLocations()
		{
			this._refreshing = true;
			this.InstallDir = this._environmentStateProvider.HomeDirectory ?? DefaultInstallationDirectory;
			this.ConfigDirectory = this._environmentStateProvider.ConfigDirectory ?? DefaultConfigDirectory;
			this.DataDirectory = this._yamlConfiguration?.Settings?.DataPath ?? DefaultDataDirectory;
			this.LogsDirectory = this._yamlConfiguration?.Settings?.LogsPath ?? DefaultLogsDirectory;

			var home = this._environmentStateProvider.HomeDirectory ?? DefaultInstallationDirectory;
			var config = this._environmentStateProvider.ConfigDirectory ?? DefaultConfigDirectory;
			var data = this._yamlConfiguration?.Settings?.DataPath ?? DefaultDataDirectory;
			var logs = this._yamlConfiguration?.Settings?.LogsPath ?? DefaultLogsDirectory;

			this.ConfigureLocations = 
				!this.SamePathAs(home, DefaultInstallationDirectory)
				|| !this.SamePathAs(config, DefaultConfigDirectory)
				|| !this.SamePathAs(data, DefaultDataDirectory)
				|| !this.SamePathAs(logs, DefaultLogsDirectory);
			this._refreshing = false;
		}

		protected bool SamePathAs(string pathA, string pathB)
		{
			if (!string.IsNullOrEmpty(pathA) && !string.IsNullOrEmpty(pathB))
			{
				var fullPathA = Path.GetFullPath(pathA).TrimEnd('\\','/');
				var fullPathB = Path.GetFullPath(pathB).TrimEnd('\\','/');
				return 0 == string.Compare(fullPathA, fullPathB, true);
			}
			else
				return false;
		}

		public void SetWritableLocationsToInstallDirectory(bool sameFolder)
		{
			if (!sameFolder) return;
			this._refreshing = true;
			this.DataDirectory = Path.Combine(this.InstallDir, Data);
			this.ConfigDirectory = Path.Combine(this.InstallDir, Config);
			this.LogsDirectory = Path.Combine(this.InstallDir, Logs);
			this._refreshing = false;
		}

		// ReactiveUI conventions do not change
		// ReSharper disable InconsistentNaming
		// ReSharper disable ArrangeTypeMemberModifiers
		readonly ObservableAsPropertyHelper<bool> configureAllLocations;
		public bool ConfigureAllLocations => configureAllLocations.Value;

		readonly ObservableAsPropertyHelper<string> elasticsearchLog;
		public string ElasticsearchLog => elasticsearchLog.Value;

		bool placeWriteableLocationsInSamePath;
		[Argument(nameof(PlaceWritableLocationsInSamePath))]
		public bool PlaceWritableLocationsInSamePath
		{
			get { return placeWriteableLocationsInSamePath; }
			set {
				this.RaiseAndSetIfChanged(ref placeWriteableLocationsInSamePath, value); 
				this.SetWritableLocationsToInstallDirectory(value);
			}

		}

		bool configureLocations;
		public bool ConfigureLocations
		{
			get { return configureLocations; }
			set { this.RaiseAndSetIfChanged(ref configureLocations, value); }
		}


		string installDirectory;
		[Argument(nameof(InstallDir))]
		public string InstallDir
		{
			get { return installDirectory; }
			set {
				this.RaiseAndSetIfChanged(ref installDirectory, value);
				this.SetWritableLocationsToInstallDirectory(this.PlaceWritableLocationsInSamePath);
			}
		}

		string dataDirectory;
		[SetPropertyActionArgument(nameof(DataDirectory), DefaultDataDirectoryArgument)]
		public string DataDirectory
		{
			get { return dataDirectory; }
			set { this.RaiseAndSetIfChanged(ref dataDirectory, value); }
		}

		string configDirectory;
		[SetPropertyActionArgument(nameof(ConfigDirectory), DefaultConfigDirectoryArgument)]
		public string ConfigDirectory
		{
			get { return configDirectory; }
			set { this.RaiseAndSetIfChanged(ref configDirectory, value); }
		}

		string logsDirectory;
		[SetPropertyActionArgument(nameof(LogsDirectory), DefaultLogsDirectoryArgument)]
		public string LogsDirectory
		{
			get { return logsDirectory; }
			set { this.RaiseAndSetIfChanged(ref logsDirectory, value); }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(LocationsModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(ConfigureAllLocations)} = " + ConfigureAllLocations);
			sb.AppendLine($"- {nameof(PlaceWritableLocationsInSamePath)} = " + PlaceWritableLocationsInSamePath);
			sb.AppendLine($"- {nameof(ConfigureLocations)} = " + ConfigureLocations);
			sb.AppendLine($"- {nameof(InstallDir)} = " + InstallDir);
			sb.AppendLine($"- {nameof(DataDirectory)} = " + DataDirectory);
			sb.AppendLine($"- {nameof(ConfigDirectory)} = " + ConfigDirectory);
			sb.AppendLine($"- {nameof(LogsDirectory)} = " + LogsDirectory);
			return sb.ToString();
		}
	}
}
