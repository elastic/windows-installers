namespace Elastic.Installer.Domain.Shared.Model.Plugins
{

	public class Plugin
	{
		public string Url { get; set; }
		public string DisplayName { get; set; }
		public string Version { get; set; }
		public string Description { get; set; }
		public bool Selected { get; set; }
		public PluginType PluginType { get; set; }
	}
}