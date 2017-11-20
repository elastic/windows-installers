using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Plugin;
using FluentValidation;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base.Plugins
{
	public abstract class PluginsModelBase<TModel, TModelValidator> : StepBase<TModel, TModelValidator>
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		public IPluginStateProvider PluginStateProvider { get; }
		protected ReactiveList<Plugin> _plugins = new ReactiveList<Plugin> { ChangeTrackingEnabled = true };

		protected bool AlreadyInstalled { get; set; }
		protected string PreviousInstallDirectory { get; set; }
		protected string ConfigDirectory { get; set; }

		protected abstract IEnumerable<Plugin> GetPlugins();

		protected virtual Dictionary<string,string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

		public ReactiveList<Plugin> AvailablePlugins
		{
			get => _plugins;
			set => this.RaiseAndSetIfChanged(ref _plugins, value);
		}

		[StaticArgument(nameof(Plugins))]
		public IEnumerable<string> Plugins
		{
			get { return _plugins.Where(p => p.Selected).Select(p => p.Url); }
			set
			{
				foreach (var p in AvailablePlugins) p.Selected = false;
				if (value == null) return;
				foreach (var p in AvailablePlugins.Where(p => value.Contains(p.Url)))
					p.Selected = true;
			}
		}

		protected PluginsModelBase(IPluginStateProvider pluginStateProvider)
		{
			this.Header = "Plugins";
			this.PluginStateProvider = pluginStateProvider;
		}

		public override void Refresh()
		{
			this.AvailablePlugins.Clear();
			var plugins = this.GetPlugins();
			this.AvailablePlugins.AddRange(plugins);

			var selectedPlugins = !this.AlreadyInstalled || string.IsNullOrEmpty(this.PreviousInstallDirectory)
				? this.DefaultPlugins()
				: this.PluginStateProvider.InstalledPlugins(this.PreviousInstallDirectory, EnvironmentVariables).ToList();
			foreach (var plugin in this.AvailablePlugins.Where(p => selectedPlugins.Contains(p.Url)))
				plugin.Selected = true;
		}

		public void ChangeXPackSelection(bool selected)
		{
			var xpackPlugin = this.AvailablePlugins.First(p => p.PluginType == PluginType.XPack);
			xpackPlugin.Selected = selected;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(this.GetType().Name);
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(Plugins)} = " + string.Join(", ", Plugins));
			return sb.ToString();
		}

		public virtual List<string> DefaultPlugins() => new List<string>();
	}
}