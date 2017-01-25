using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Service
{
	public abstract class ServiceConfiguration
	{
		public string ExeLocation { get; set; }
		public string EventLogSource { get; set; }
		public ServiceAccount ServiceAccount { get; set; }
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public ServiceStartMode StartMode { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
	}
}
