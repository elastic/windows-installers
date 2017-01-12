using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Properties;
using ReactiveUI;
using Elastic.Installer.Domain.Shared.Configuration;
using FluentValidation;
using Elastic.Installer.Domain.Shared.Model.Plugins;

namespace Elastic.Installer.Domain.Shared.Model.Plugins
{
	public abstract class PluginsModelBase<TModel, TModelValidator> : StepBase<TModel, TModelValidator>
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		public IPluginStateProvider PluginStateProvider { get; }
		private ReactiveList<Plugin> _plugins = new ReactiveList<Plugin> { ChangeTrackingEnabled = true };

		protected abstract IEnumerable<Plugin> GetPlugins();

		public ReactiveList<Plugin> AvailablePlugins
		{
			get { return _plugins; }
			set { this.RaiseAndSetIfChanged(ref _plugins, value); }
		}

		[StaticArgument(nameof(Plugins))]
		public IEnumerable<string> Plugins
		{
			get { return _plugins.Where(p => p.Selected).Select(p => p.Url); }
			set
			{
				foreach (var p in AvailablePlugins) p.Selected = false;
				if (value == null) return;
				var plugins = AvailablePlugins.Where(p => value.Contains(p.Url)).ToList();
				foreach (var p in plugins) p.Selected = true;
			}
		}

		public PluginsModelBase(IPluginStateProvider pluginStateProvider)
		{
			this.Header = "Plugins";
			this.PluginStateProvider = pluginStateProvider;
		}

		public override void Refresh()
		{
			this.AvailablePlugins.Clear();
			var plugins = this.GetPlugins();
			this.AvailablePlugins.AddRange(plugins);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(this.GetType().Name);
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(Plugins)} = " + string.Join(", ", Plugins));
			return sb.ToString();
		}
	}
}