using Elastic.Installer.Domain.Properties;

namespace Elastic.Installer.Domain.Model.Base.Plugins
{
	public class PluginType
	{
		public string Name { get; private set; }

		public static PluginType Analysis = new PluginType(TextResources.PluginType_Analysis);
		public static PluginType Discovery = new PluginType(TextResources.PluginType_Discovery);
		public static PluginType Snapshot = new PluginType(TextResources.PluginType_Snapshot);
		public static PluginType Scripting = new PluginType(TextResources.PluginType_Scripting);
		public static PluginType XPack = new PluginType(TextResources.PluginType_XPack);
		public static PluginType ApiExtensions = new PluginType(TextResources.PluginType_ApiExtensions);
		public static PluginType Ingest = new PluginType(TextResources.PluginType_Ingest);
		public static PluginType Mapper = new PluginType(TextResources.PluginType_Mapper);
		public static PluginType Store = new PluginType(TextResources.PluginType_Store);

		protected PluginType(string name)
		{
			this.Name = name;
		}
	}
}