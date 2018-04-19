using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Shared
{
	public class LicenseModel
	{
		public LicenseModel()
		{
			this.OpenLicense = ReactiveCommand.Create();
			this.Close = ReactiveCommand.Create();
		}

		public ReactiveCommand<object> Close { get; }

		public ReactiveCommand<object> OpenLicense { get; }
	}
}