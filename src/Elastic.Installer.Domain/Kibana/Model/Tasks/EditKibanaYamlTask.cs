using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Model.Tasks
{
	public class EditKibanaYamlTask : KibanaInstallationTask
	{
		public EditKibanaYamlTask(string[] args, ISession session) : base(args, session) { }
		public EditKibanaYamlTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			throw new NotImplementedException();
		}
	}
}
