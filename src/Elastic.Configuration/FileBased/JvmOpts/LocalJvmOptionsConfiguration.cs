using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

namespace Elastic.Configuration.FileBased.JvmOpts
{
	public class LocalJvmOptionsConfiguration
	{
		// Example jvm.options line for GC log directory:
		// 9-:-Xlog:gc*,gc+age=trace,safepoint:file=logs/gc.log:utctime,pid,tags:filecount=32,filesize=64m
		private static readonly Regex LogsPathRegEx = new Regex(@"file\=(?<Filepath>.*?)\:", RegexOptions.Compiled);
		private static readonly string LogsEnvironmentVariable = "${ES_PATH_LOGS}";

		private readonly string _path;
		private readonly List<string> _options = new List<string>();
		private readonly IFileSystem _fileSystem;

		public ReadOnlyCollection<string> Options => new ReadOnlyCollection<string>(this._options ?? new List<string>());

		public string Xmx { get; set; }
		public string Xms { get; set; }

		public ulong? ConfiguredHeapSize { get; }

		public LocalJvmOptionsConfiguration(string path) : this(path, null) { }
		public LocalJvmOptionsConfiguration(string path, IFileSystem fileSystem)
		{
			this._fileSystem = fileSystem ?? new FileSystem();
			this._path = path;
			if (string.IsNullOrEmpty(path) || !this._fileSystem.File.Exists(this._path)) return;

			this._options = this._fileSystem.File.ReadAllLines(this._path).ToList();

			foreach (var l in _options)
			{
				if (l.StartsWith("-Xmx")) this.Xmx = l.Replace("-Xmx", "");
				if (l.StartsWith("-Xms")) this.Xms = l.Replace("-Xms", "");
			}

			if (!string.IsNullOrEmpty(this.Xmx) && ulong.TryParse(this.Xmx.Replace("m", ""), out var heap))
				this.ConfiguredHeapSize = heap;
		}

		public void Save()
		{
			var write = new List<string>();

			foreach (var option in _options)
			{
				if (option.StartsWith("-Xmx") || option.StartsWith("-Xms"))
				{
					continue;
				}
				else if (option.StartsWith("9-:-Xlog:gc") && option.Contains("file=logs/gc.log"))
				{
					var filePath = LogsPathRegEx.Match(option).Groups["Filepath"].Value;
					// Does the path already include the rewritten environment variable?
					if (filePath.Contains(LogsEnvironmentVariable))
					{
						// Write as-is and continue
						write.Add(option);
						continue;
					}
					var filenameIndex = filePath.LastIndexOf("/");
					var filename = filePath;
					if (filenameIndex > 0)
					{
						filename = filePath.Substring(filenameIndex);
					}
					var rewrittenLine = option.Replace(filePath, @"""" + LogsEnvironmentVariable + filename + @"""");
					write.Add(rewrittenLine);
				}
				else
				{
					write.Add(option);
				}
			}
			if (!string.IsNullOrEmpty(this.Xmx)) write.Add($"-Xmx{this.Xmx}");
			if (!string.IsNullOrEmpty(this.Xms)) write.Add($"-Xms{this.Xms}");
			this._fileSystem.File.WriteAllLines(this._path, write);
		}

		public override string ToString() => string.Join(" ", this._options);

		public static LocalJvmOptionsConfiguration FromFolder(string configDirectory) => FromFolder(configDirectory, null);

		public static LocalJvmOptionsConfiguration FromFolder(string configDirectory, IFileSystem fileSystem) =>
			string.IsNullOrEmpty(configDirectory)
			? new LocalJvmOptionsConfiguration(null)
			: new LocalJvmOptionsConfiguration(Path.Combine(configDirectory, "jvm.options"), fileSystem);
	}
}
