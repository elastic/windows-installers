using System;
using System.IO;
using System.Linq;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Kibana.Model.Locations
{
	public class LocationsModelValidator : AbstractValidator<LocationsModel>
	{
		public static readonly string DirectoryMustBeSpecified = TextResources.LocationsModelValidator_DirectoryMustBeSpecified;
		public static readonly string DirectoryMustNotBeRelative = TextResources.LocationsModelValidator_DirectoryMustNotBeRelative;
		public static readonly string DirectoryUsesUnknownDrive = TextResources.LocationsModelValidator_DirectoryUsesUnknownDrive;
		public static readonly string DirectorySetToNonWritableLocation = TextResources.LocationsModelValidator_DirectorySetToNonWritableLocation;
		public static readonly string DirectoryMustBeChildOf = TextResources.LocationsModelValidator_DirectoryMustBeChildOf;
		public static readonly string Installation = TextResources.LocationsModelValidator_Installation;
		public static readonly string ConfigurationText = TextResources.LocationsModelValidator_Configuration;
		public static readonly string DataText = TextResources.LocationsModelValidator_Data;
		public static readonly string LogsText = TextResources.LocationsModelValidator_Logs;

		public LocationsModelValidator()
		{
			RuleFor(vm => vm.InstallDir)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty().WithMessage(DirectoryMustBeSpecified, Installation)
				.Must(this.MustBeRooted).WithMessage(DirectoryMustNotBeRelative, Installation)
				.Must(this.InstallOnKnownDrive)
				.WithMessage(DirectoryUsesUnknownDrive, x => Installation, x => new DirectoryInfo(x.InstallDir).Root.Name);

			RuleFor(vm => vm.LogsDirectory)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty().WithMessage(DirectoryMustBeSpecified, LogsText)
				.Must(this.MustBeRooted).WithMessage(DirectoryMustNotBeRelative, LogsText)
				.Must(this.InstallOnKnownDrive)
				.WithMessage(DirectoryUsesUnknownDrive, x => LogsText, x => new DirectoryInfo(x.LogsDirectory).Root.Name)
				.Must(this.NotBeChildOfProgramFiles).WithMessage(DirectorySetToNonWritableLocation, LogsText);

			RuleFor(vm => vm.LogsDirectory)
				.Must((vm, logs) => this.IsSubPathOf(vm.InstallDir, logs))
				.When(vm => vm.PlaceWritableLocationsInSamePath)
				.WithMessage(DirectoryMustBeChildOf, vm => LogsText, vm => vm.InstallDir);
		}

		public bool MustBeRooted(string path)
		{
			var isRooted = Path.IsPathRooted(path);
			return isRooted;
		}

		public bool InstallOnKnownDrive(string path)
		{
			var pathInfo = new DirectoryInfo(path);
			var drive = pathInfo.Root;
			var drives = DriveInfo.GetDrives();
			return drives.Any(d => string.Equals(d.Name, drive.Name, StringComparison.InvariantCultureIgnoreCase));
		}

		private static readonly string X86ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		private static readonly string ProgramFiles = LocationsModel.DefaultProgramFiles;

		public bool NotBeChildOfProgramFiles(string path) =>
			!this.IsSubPathOf(X86ProgramFiles, path) && !this.IsSubPathOf(ProgramFiles, path);

		public bool IsSubPathOf(string badParent, string path)
		{
			if (!this.MustBeRooted(badParent) || !this.MustBeRooted(path)) return true;
			string parent = Path.GetFullPath(badParent);
			string child = Path.GetFullPath(path);

			return path.StartsWith(badParent, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
