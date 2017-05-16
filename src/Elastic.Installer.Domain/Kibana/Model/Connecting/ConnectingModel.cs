using Elastic.Installer.Domain.Model;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Model.Connecting
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
			get { return this.url; }
			set { this.RaiseAndSetIfChanged(ref this.url, value); }
		}

		string indexName;
		[StaticArgument(nameof(IndexName))]
		public string IndexName
		{
			get { return this.indexName; }
			set { this.RaiseAndSetIfChanged(ref this.indexName, value); }
		}

		string username;
		[StaticArgument(nameof(ElasticsearchUsername))]
		public string ElasticsearchUsername
		{
			get { return this.username; }
			set { this.RaiseAndSetIfChanged(ref this.username, value); }
		}

		string password;
		[StaticArgument(nameof(ElasticsearchPassword))]
		public string ElasticsearchPassword
		{
			get { return this.password; }
			set { this.RaiseAndSetIfChanged(ref this.password, value); }
		}

		string elasticsearchCert;
		[StaticArgument(nameof(ElasticsearchCert))]
		public string ElasticsearchCert
		{
			get { return this.elasticsearchCert; }
			set { this.RaiseAndSetIfChanged(ref this.elasticsearchCert, value); }
		}

		string elasticsearchKey;
		[StaticArgument(nameof(ElasticsearchKey))]
		public string ElasticsearchKey
		{
			get { return this.elasticsearchKey; }
			set { this.RaiseAndSetIfChanged(ref this.elasticsearchKey, value); }
		}

		string elasticsearchCA;
		[StaticArgument(nameof(ElasticsearchCA))]
		public string ElasticsearchCA
		{
			get { return this.elasticsearchCA; }
			set { this.RaiseAndSetIfChanged(ref this.elasticsearchCA, value); }
		}
	}
}
