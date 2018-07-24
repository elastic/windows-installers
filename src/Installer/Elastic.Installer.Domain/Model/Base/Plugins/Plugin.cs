using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base.Plugins
{
	public class Plugin : ReactiveObject
	{
		private bool _selected;

		/// <summary>
		/// The original input received for the plugin
		/// </summary>
		public string Input { get; set; }

		/// <summary>
		/// The url from which to download the plugin
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// The plugin display name
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// The plugin version
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// The plugin description
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Whether the plugin is selected for installation
		/// </summary>
		public bool Selected
		{
			get => _selected;
			set => this.RaiseAndSetIfChanged(ref _selected, value);
		}

		/// <summary>
		/// The type of plugin
		/// </summary>
		public PluginType PluginType { get; set; }
	}
}