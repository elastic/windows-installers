using System;
using System.IO;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Model.Base;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Kibana.Locations
{
	public class LocationsModel : StepBase<LocationsModel, LocationsModelValidator>
	{
		private const string ProgramDataEnvironmentVariable = "ALLUSERSPROFILE";
		private const string Logs = "logs";
		private const string Config = "config";
		private const string DefaultWritableDirectoryArgument = 
			@"[%" + ProgramDataEnvironmentVariable + @"]\" + CompanyFolderName + @"\" + ProductFolderName;
		private const string DefaultLogsDirectoryArgument = DefaultWritableDirectoryArgument + @"\" + Logs;
		// TODO add this to UI
		private const string DefaultConfigDirectoryArgument = DefaultWritableDirectoryArgument + @"\" + Config;
		public const string CompanyFolderName = "Elastic";
		public const string ProductFolderName = "Kibana";

		public static string DefaultProgramFiles => 
			Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

		public static readonly string DefaultCompanyDirectory =
			Path.Combine(Environment.GetEnvironmentVariable(ProgramDataEnvironmentVariable), CompanyFolderName);

		public static readonly string DefaultWritableDirectory = Path.Combine(DefaultCompanyDirectory, ProductFolderName);

		public static readonly string DefaultInstallationDirectory = Path.Combine(DefaultProgramFiles, CompanyFolderName, ProductFolderName);
		public static readonly string DefaultMsiLogFileLocation = Path.Combine(DefaultWritableDirectory, "install.log");
		public static readonly string DefaultLogsDirectory = Path.Combine(DefaultWritableDirectory, Logs);

		private bool _refreshing;

		public LocationsModel(VersionConfiguration versionConfig)
		{
			this.IsRelevant = !versionConfig.ExistingVersionInstalled;
			this.Header = "Locations";

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
						return Path.Combine(c.GetValue(), "kibana.log");
					return null;
				})
				.ToProperty(this, vm => vm.KibanaLog, out kibanaLog);

			this.ThrownExceptions.Subscribe(e =>
			{
			});

			//If install, config, logs or data dir are set force ConfigureLocations to true
			this.WhenAny(
				vm => vm.InstallDir,
				vm => vm.LogsDirectory,
				(i, l) => !this._refreshing
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
			this.InstallDir = DefaultInstallationDirectory;
			this.LogsDirectory = DefaultLogsDirectory;

			var home = DefaultInstallationDirectory;
			var logs = DefaultLogsDirectory;

			this.ConfigureLocations = 
				!this.SamePathAs(home, DefaultInstallationDirectory)
				|| !this.SamePathAs(logs, DefaultLogsDirectory);
			this._refreshing = false;
		}

		protected bool SamePathAs(string pathA, string pathB)
		{
			if (!string.IsNullOrEmpty(pathA) && !string.IsNullOrEmpty(pathB))
			{
				var fullPathA = Path.GetFullPath(pathA).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				var fullPathB = Path.GetFullPath(pathB).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				return 0 == string.Compare(fullPathA, fullPathB, StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		public void SetWritableLocationsToInstallDirectory(bool sameFolder)
		{
			if (!sameFolder) return;
			this._refreshing = true;
			this.LogsDirectory = Path.Combine(this.InstallDir, Logs);
			this._refreshing = false;
		}

		// ReactiveUI conventions do not change
		// ReSharper disable InconsistentNaming
		// ReSharper disable ArrangeTypeMemberModifiers
		readonly ObservableAsPropertyHelper<bool> configureAllLocations;
		public bool ConfigureAllLocations => configureAllLocations.Value;

		readonly ObservableAsPropertyHelper<string> kibanaLog;
		public string KibanaLog => kibanaLog.Value;

		bool placeWriteableLocationsInSamePath;
		[Argument(nameof(PlaceWritableLocationsInSamePath))]
		public bool PlaceWritableLocationsInSamePath
		{
			get => placeWriteableLocationsInSamePath;
			set {
				this.RaiseAndSetIfChanged(ref placeWriteableLocationsInSamePath, value); 
				this.SetWritableLocationsToInstallDirectory(value);
			}

		}

		bool configureLocations;
		public bool ConfigureLocations
		{
			get => configureLocations;
			set => this.RaiseAndSetIfChanged(ref configureLocations, value);
		}

		string installDirectory;
		[Argument(nameof(InstallDir))]
		public string InstallDir
		{
			get => installDirectory;
			set {
				this.RaiseAndSetIfChanged(ref installDirectory, value);
				this.SetWritableLocationsToInstallDirectory(this.PlaceWritableLocationsInSamePath);
			}
		}

		string configDirectory;
		[SetPropertyActionArgument(nameof(ConfigDirectory), DefaultConfigDirectoryArgument)]
		public string ConfigDirectory
		{
			get => configDirectory;
			set => this.RaiseAndSetIfChanged(ref configDirectory, value);
		}

		string logsDirectory;
		[SetPropertyActionArgument(nameof(LogsDirectory), DefaultLogsDirectoryArgument)]
		public string LogsDirectory
		{
			get => logsDirectory;
			set => this.RaiseAndSetIfChanged(ref logsDirectory, value);
		}

		public string LogsFile => LogsDirectory.Equals("stdout", StringComparison.OrdinalIgnoreCase) 
			? LogsDirectory 
			: Path.Combine(LogsDirectory, "kibana.log");

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(LocationsModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(ConfigureAllLocations)} = " + ConfigureAllLocations);
			sb.AppendLine($"- {nameof(PlaceWritableLocationsInSamePath)} = " + PlaceWritableLocationsInSamePath);
			sb.AppendLine($"- {nameof(ConfigureLocations)} = " + ConfigureLocations);
			sb.AppendLine($"- {nameof(InstallDir)} = " + InstallDir);
			sb.AppendLine($"- {nameof(LogsDirectory)} = " + LogsDirectory);
			sb.AppendLine($"- {nameof(ConfigDirectory)} = " + ConfigDirectory);
			return sb.ToString();
		}
	}
}
