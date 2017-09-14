using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.Domain.Model.Base;
using FluentValidation.Results;

namespace Elastic.InstallerHosts.Tasks
{
	public abstract class InstallationTaskBase
	{
		protected IValidatableReactiveObject Model { get; }

		protected IFileSystem FileSystem { get; set; }

		protected ISession Session { get; set; }

		protected string[] Args { get; set; }

		protected string[] SanitizedArgs => 
			Args.Select(a =>
			{
				var parts = a.Split(new[] {'='}, 2);
				return Model.HiddenProperties.Contains(parts[0]) ? $"{parts[0]}=**********" : a;
			}).ToArray();

		protected string ActionName => this.GetType().FullName;

		protected abstract bool ExecuteTask();

		protected InstallationTaskBase(IValidatableReactiveObject model, ISession session, IFileSystem fileSystem)
		{
			this.Model = model;
			this.Session = session;
			this.FileSystem = fileSystem;
			this.Args = new string[] { };
		}

		public bool Execute()
		{
			if (this.Model.IsValid) return this.ExecuteTask();
			
			var errorPrefix = $"Can not execute {ActionName} the model that it was passed has the following errors";
			var validationFailures = ValidationFailures(this.Model.ValidationFailures);
			throw new Exception(errorPrefix + Environment.NewLine + validationFailures);
		}

		public string ValidationFailures(IList<ValidationFailure> f) =>
			f.Aggregate(new StringBuilder(), (sb, v) =>
				sb.AppendLine($"{v.PropertyName.ToUpperInvariant().ValidationMessage()}: {v.ErrorMessage}"), sb => sb.ToString());

		protected bool SamePathAs(string pathA, string pathB) => 
			!string.IsNullOrEmpty(pathA) && 
			!string.IsNullOrEmpty(pathB) && 
			0 == string.Compare(Path.GetFullPath(pathA), Path.GetFullPath(pathB), true);

		protected void CopyDirectory(string sourceDirectory, string destinationDirectory)
		{
			var source = this.FileSystem.DirectoryInfo.FromDirectoryName(sourceDirectory);
			var destination = this.FileSystem.DirectoryInfo.FromDirectoryName(destinationDirectory);
			CopyDirectory(source, destination);
		}
		
		protected void CopyDirectory(DirectoryInfoBase source, DirectoryInfoBase target)
		{
			var fs = this.FileSystem;
			fs.Directory.CreateDirectory(target.FullName);

			foreach (var file in source.GetFiles())
				fs.File.Copy(file.FullName, fs.Path.Combine(target.FullName, file.Name), true);

			foreach (var directory in source.GetDirectories())
			{
				var nextTargetSubDir = target.CreateSubdirectory(directory.Name);
				CopyDirectory(directory, nextTargetSubDir);
			}
		}
	}
}
