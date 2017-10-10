using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class SetBootstrapPasswordPropertyTask : ElasticsearchInstallationTaskBase
	{
		private static readonly char[] BootstrapChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*-_=+?".ToCharArray();

		public SetBootstrapPasswordPropertyTask(string[] args, ISession session) 
			: base(args, session) { }

		public SetBootstrapPasswordPropertyTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var length = 20;
			var password = new StringBuilder();

			int GetValidIndex(RandomNumberGenerator rng, int max)
			{
				var randomBytes = new byte[4];
				int value;
				do
				{
					rng.GetBytes(randomBytes);
					value = BitConverter.ToInt32(randomBytes, 0) & int.MaxValue;
				} while (value >= max * (int.MaxValue / max));

				return value % max;
			}

			using (var rng = new RNGCryptoServiceProvider())
			{
				for (var i = 0; i < 20; i++)
					password.Append(BootstrapChars[GetValidIndex(rng, BootstrapChars.Length)]);
			}
		
			Session.Set("BOOTSTRAPPASSWORD", password.ToString());
			return true;
		}
	}
}