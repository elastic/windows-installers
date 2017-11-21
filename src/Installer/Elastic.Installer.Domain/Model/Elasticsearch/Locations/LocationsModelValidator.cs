using System;
using System.IO;
using System.Linq;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Locations
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

			RuleFor(vm => vm.ConfigDirectory)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty().WithMessage(DirectoryMustBeSpecified, ConfigurationText)
				.Must(this.MustBeRooted).WithMessage(DirectoryMustNotBeRelative, ConfigurationText)
				.Must(this.InstallOnKnownDrive)
				.WithMessage(DirectoryUsesUnknownDrive, x => ConfigurationText, x => new DirectoryInfo(x.ConfigDirectory).Root.Name)
				.Must(this.NotBeChildOfProgramFiles).WithMessage(DirectorySetToNonWritableLocation, ConfigurationText);

			RuleFor(vm => vm.ConfigDirectory)
				.Must((vm, conf) => this.IsSubPathOf(vm, vm.InstallDir, conf))
				.When(vm => vm.PlaceWritableLocationsInSamePath)
				.WithMessage(DirectoryMustBeChildOf, vm => ConfigurationText, vm => vm.InstallDir);

			RuleFor(vm => vm.DataDirectory)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty().WithMessage(DirectoryMustBeSpecified, DataText)
				.Must(this.MustBeRooted).WithMessage(DirectoryMustNotBeRelative, DataText)
				.Must(this.InstallOnKnownDrive)
				.WithMessage(DirectoryUsesUnknownDrive, x => DataText, x => new DirectoryInfo(x.DataDirectory).Root.Name)
				.Must(this.NotBeChildOfProgramFiles).WithMessage(DirectorySetToNonWritableLocation, DataText);

			RuleFor(vm => vm.DataDirectory)
				.Must((vm, data) => this.IsSubPathOf(vm, vm.InstallDir, data))
				.When(vm => vm.PlaceWritableLocationsInSamePath)
				.WithMessage(DirectoryMustBeChildOf, vm => DataText, vm => vm.InstallDir);

			RuleFor(vm => vm.LogsDirectory)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty().WithMessage(DirectoryMustBeSpecified, LogsText)
				.Must(this.MustBeRooted).WithMessage(DirectoryMustNotBeRelative, LogsText)
				.Must(this.InstallOnKnownDrive)
				.WithMessage(DirectoryUsesUnknownDrive, x => LogsText, x => new DirectoryInfo(x.LogsDirectory).Root.Name)
				.Must(this.NotBeChildOfProgramFiles).WithMessage(DirectorySetToNonWritableLocation, LogsText);

			RuleFor(vm => vm.LogsDirectory)
				.Must((vm, logs) => this.IsSubPathOf(vm, vm.InstallDir, logs))
				.When(vm => vm.PlaceWritableLocationsInSamePath)
				.WithMessage(DirectoryMustBeChildOf, vm => LogsText, vm => vm.InstallDir);
		}

		public bool MustBeRooted(LocationsModel model, string path)
		{
			var isRooted = model.FileSystem.Path.IsPathRooted(path);
			return isRooted;
		}

		public bool InstallOnKnownDrive(LocationsModel model, string path)
		{
			var fileSystem = model.FileSystem;
			var pathInfo = fileSystem.DirectoryInfo.FromDirectoryName(path);
			var root = pathInfo.Root;
			var drives = fileSystem.DriveInfo.GetDrives();
			return drives.Any(d => string.Equals(d.Name, root.FullName, StringComparison.InvariantCultureIgnoreCase));
		}

		private static readonly string X86ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		private static readonly string ProgramFiles = LocationsModel.DefaultProgramFiles;

		public bool NotBeChildOfProgramFiles(LocationsModel model, string path) =>
			!this.IsSubPathOf(model, X86ProgramFiles, path) && !this.IsSubPathOf(model, ProgramFiles, path);

		public bool IsSubPathOf(LocationsModel model, string badParent, string path)
		{
			if (!this.MustBeRooted(model, badParent) || !this.MustBeRooted(model, path)) return true;
			return path.StartsWith(badParent, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
