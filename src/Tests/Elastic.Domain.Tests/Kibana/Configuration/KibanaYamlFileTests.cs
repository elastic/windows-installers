using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Configuration.FileBased.Yaml;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Kibana.Configuration
{
	public class KibanaYamlFileTests
	{
		private string _path = "C:\\Java\\kibana.yaml";

		[Fact]
		void KnownSettingsAreReadCorrectly()
		{
			var folder = @"C:\ProgramData\Elastic\Kibana\";
			var loggingDest = $@"{folder}\logs";
			var serverHost = "localhost";
			var serverPort = 5601;
			var basePath = "/";
			var serverName = "my-hostname";
			var defaultRoute = "/app/kibana";
			var esUrl = "http://localhost:9200";
			var kibanaIndex = ".kibana";
			var serverCert = @"C:\servercert";
			var serverKey = @"C:\serverkey";
			var esCert = @"C:\escert";
			var esKey = @"C:\eskey";
			var esCa = @"C:\esca";

			var yaml = $@"elasticsearch.ssl.ca: {esCa}
elasticsearch.ssl.cert: {esCert}
elasticsearch.ssl.key: {esKey}
elasticsearch.url: {esUrl}
kibana.index: {kibanaIndex}
logging.dest: {loggingDest}
server.basePath: {basePath}
server.defaultRoute: {defaultRoute}
server.host: {serverHost}
server.name: {serverName}
server.port: {serverPort}
server.ssl.cert: {serverCert}
server.ssl.key: {serverKey}
status.allowAnonymous: true
";
			var fs = FakeYaml(yaml);
			var optsFile = new KibanaYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.ServerPort.Should().Be(serverPort);
			settings.ServerHost.Should().Be(serverHost);
			settings.ServerBasePath.Should().Be(basePath);
			settings.ServerDefaultRoute.Should().Be(defaultRoute);
			settings.ElasticsearchUrl.Should().Be(esUrl);
			settings.KibanaIndex.Should().Be(kibanaIndex);
			settings.ServerCert.Should().Be(serverCert);
			settings.ServerKey.Should().Be(serverKey);
			settings.ElasticsearchCert.Should().Be(esCert);
			settings.ElasticsearchKey.Should().Be(esKey);
			settings.ElasticsearchCA.Should().Be(esCa);
			settings.LoggingDestination.Should().Be(loggingDest);
			settings.AllowAnonymousAccess.Should().Be(true);
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(_path);
			fileContentsAfterSave.Replace("\r", "").Should().Be(yaml.Replace("\r", ""));

		}

		private MockFileSystem FakeYaml(string yaml)
		{
			var fs = new MockFileSystem(new Dictionary<string, MockFileData>
			{
				{_path, new MockFileData(yaml)}
			});
			return fs;
		}
	}
}
