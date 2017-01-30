using Elastic.Installer.Domain.Process.ObservableWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Process
{
	public abstract class ServiceProcess : IDisposable
	{
		protected ObservableProcess _process;
		protected CompositeDisposable _disposables = new CompositeDisposable();

		protected abstract List<string> ParseArguments(IEnumerable<string> args);
		protected abstract List<string> GetArguments();

		public string ProcessExe { get; protected set; }
		public bool Started { get; protected set; }
		public string HomeDirectory { get; protected set; }
		public string ConfigDirectory { get; protected set; }

		public ServiceProcess(IEnumerable<string> args)
		{

		}

		public virtual void Start()
		{

		}

		public virtual void Stop()
		{

		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
