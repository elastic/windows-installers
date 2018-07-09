using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base.Plugins
{
	public class Plugin : ReactiveObject
	{
		private bool _selected;

		public string Input { get; set; }
		public string Url { get; set; }
		public string DisplayName { get; set; }
		public string Version { get; set; }
		public string Description { get; set; }

		public bool Selected
		{
			get => _selected;
			set => this.RaiseAndSetIfChanged(ref _selected, value);
		}

		public PluginType PluginType { get; set; }
	}
}