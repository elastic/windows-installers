using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration
{
	public class LocalJvmOptionsFileTests
	{

		[Fact] void MemoryIsReadUpdatedAndSavedOK()
		{
			var jvmOpts = $@"-Xms1024m
-Xmx8146m
-XX:+UseParNewGC
-XX:+UseConcMarkSweepGC
-XX:CMSInitiatingOccupancyFraction=75
-XX:+UseCMSInitiatingOccupancyOnly
-XX:+DisableExplicitGC
-XX:+AlwaysPreTouch
-Djava.awt.headless=true
-Dfile.encoding=UTF-8
-Djna.nosys=true
-XX:+HeapDumpOnOutOfMemoryError
";
			var path = "C:\\Java\\jvm.options";
			var fs = new MockFileSystem(new Dictionary<string, MockFileData>
			{
				{ path , new MockFileData(jvmOpts) }
			});
			var optsFile = new LocalJvmOptionsConfiguration(path, fs);
			optsFile.Xms.Should().Be("1024m");
			optsFile.Xmx.Should().Be("8146m");

			optsFile.Xms = optsFile.Xmx;
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(path);
			fileContentsAfterSave.Replace("\r", "").Should().Be($@"-XX:+UseParNewGC
-XX:+UseConcMarkSweepGC
-XX:CMSInitiatingOccupancyFraction=75
-XX:+UseCMSInitiatingOccupancyOnly
-XX:+DisableExplicitGC
-XX:+AlwaysPreTouch
-Djava.awt.headless=true
-Dfile.encoding=UTF-8
-Djna.nosys=true
-XX:+HeapDumpOnOutOfMemoryError
-Xmx8146m
-Xms8146m
".Replace("\r", ""));

		}

		[Fact] void SettingMemoryToNull()
		{
			var jvmOpts = $@"-XX:+UseParNewGC
-XX:+UseConcMarkSweepGC
-XX:CMSInitiatingOccupancyFraction=75
-XX:+UseCMSInitiatingOccupancyOnly
-XX:+DisableExplicitGC
-XX:+AlwaysPreTouch
-Djava.awt.headless=true
-Dfile.encoding=UTF-8
-Djna.nosys=true
-XX:+HeapDumpOnOutOfMemoryError
-Xmx8146m
-Xms1024m
";
			var path = "C:\\Java\\jvm.options";
			var fs = new MockFileSystem(new Dictionary<string, MockFileData>
			{
				{ path , new MockFileData(jvmOpts) }
			});
			var optsFile = new LocalJvmOptionsConfiguration(path, fs);
			optsFile.Xms.Should().Be("1024m");
			optsFile.Xmx.Should().Be("8146m");

			optsFile.Xms = null;
			optsFile.Xmx = null;
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(path);

			var jvmOptsAfterSave = $@"-XX:+UseParNewGC
-XX:+UseConcMarkSweepGC
-XX:CMSInitiatingOccupancyFraction=75
-XX:+UseCMSInitiatingOccupancyOnly
-XX:+DisableExplicitGC
-XX:+AlwaysPreTouch
-Djava.awt.headless=true
-Dfile.encoding=UTF-8
-Djna.nosys=true
-XX:+HeapDumpOnOutOfMemoryError
";
			fileContentsAfterSave.Replace("\r", "").Should().Be(jvmOptsAfterSave.Replace("\r", ""));

		}
	}
}
