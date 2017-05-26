using Elastic.Installer.Domain.Model.Base;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Kibana.Connecting
{
	public class ConnectingModel : StepBase<ConnectingModel, ConnectingModelValidator>
	{
		public const string DefaultUrl = "http://localhost:9200";
		public const string DefaultIndexName = ".kibana";
		public const string DefaultUserName = "elastic";
		public const string DefaultPassword = "changeMe";

		public ConnectingModel()
		{
			this.Header = "Connecting";
		}

		public override void Refresh()
		{
			this.Url = DefaultUrl;
			this.IndexName = DefaultIndexName;
			this.ElasticsearchUsername = DefaultUserName;
			this.ElasticsearchPassword = DefaultPassword;
		}

		string url;
		[StaticArgument(nameof(Url))]
		public string Url
		{
			get => this.url;
			set => this.RaiseAndSetIfChanged(ref this.url, value);
		}

		string indexName;
		[StaticArgument(nameof(IndexName))]
		public string IndexName
		{
			get => this.indexName;
			set => this.RaiseAndSetIfChanged(ref this.indexName, value);
		}

		string username;
		[StaticArgument(nameof(ElasticsearchUsername))]
		public string ElasticsearchUsername
		{
			get => this.username;
			set => this.RaiseAndSetIfChanged(ref this.username, value);
		}

		string password;
		[StaticArgument(nameof(ElasticsearchPassword))]
		public string ElasticsearchPassword
		{
			get => this.password;
			set => this.RaiseAndSetIfChanged(ref this.password, value);
		}

		string elasticsearchCert;
		[StaticArgument(nameof(ElasticsearchCert))]
		public string ElasticsearchCert
		{
			get => this.elasticsearchCert;
			set => this.RaiseAndSetIfChanged(ref this.elasticsearchCert, value);
		}

		string elasticsearchKey;
		[StaticArgument(nameof(ElasticsearchKey))]
		public string ElasticsearchKey
		{
			get => this.elasticsearchKey;
			set => this.RaiseAndSetIfChanged(ref this.elasticsearchKey, value);
		}

		string elasticsearchCA;
		[StaticArgument(nameof(ElasticsearchCA))]
		public string ElasticsearchCA
		{
			get => this.elasticsearchCA;
			set => this.RaiseAndSetIfChanged(ref this.elasticsearchCA, value);
		}
	}
}
