using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elastic.Installer.Domain
{
	public class ElasticsearchNodeInfo
	{
		public string Version { get; }
		public int? Pid { get; }
		public string Build { get; }

		public ElasticsearchNodeInfo(string version, string pid, string build)
		{
			this.Version = version;
			if (!string.IsNullOrEmpty(pid))
				Pid = int.Parse(pid);
			Build = build;
		}

	}
}
