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
		public ConnectingModel()
		{
			this.Header = "Connecting";
		}

		public override void Refresh()
		{
		}
	}
}
