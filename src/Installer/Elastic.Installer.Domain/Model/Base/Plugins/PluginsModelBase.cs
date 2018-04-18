using System;
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
		protected PluginsModelBase(IPluginStateProvider pluginStateProvider)
		{
			this.Header = "Plugins";
			this.PluginStateProvider = pluginStateProvider;
			this.InstalledPlugins.Changed.Subscribe(e => this.InstalledPlugins.RaisePropertyChanged());
		}
		
		public IPluginStateProvider PluginStateProvider { get; }
		protected ReactiveList<Plugin> _availablePlugins = new ReactiveList<Plugin> { ChangeTrackingEnabled = true };
		protected ReactiveList<string> _installedPlugins = new ReactiveList<string> { ChangeTrackingEnabled = true };

		protected bool AlreadyInstalled { get; set; }
		protected string PreviousInstallDirectory { get; set; }
		protected string ConfigDirectory { get; set; }

		protected abstract IEnumerable<Plugin> GetPlugins();

		protected virtual Dictionary<string,string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

		public ReactiveList<Plugin> AvailablePlugins
		{
			get => _availablePlugins;
			set => this.RaiseAndSetIfChanged(ref _availablePlugins, value);
		}

		[StaticArgument(nameof(Plugins))]
		public IEnumerable<string> Plugins
		{
			get { return _availablePlugins.Where(p => p.Selected).Select(p => p.Url); }
			set
			{
				foreach (var p in AvailablePlugins) p.Selected = false;
				if (value == null) return;
				foreach (var p in AvailablePlugins.Where(p => value.Contains(p.Url)))
					p.Selected = true;
			}
		}

		public ReactiveList<string> InstalledPlugins
		{
			get => _installedPlugins;
			set => this.RaiseAndSetIfChanged(ref _installedPlugins, value);
		}


		public override void Refresh()
		{
			this.AvailablePlugins.Clear();
			this.InstalledPlugins.Clear();
			this.AvailablePlugins.AddRange(this.GetPlugins());

			var installedPlugins = !this.AlreadyInstalled || string.IsNullOrEmpty(this.PreviousInstallDirectory)
				? null
				: this.PluginStateProvider.InstalledPlugins(this.PreviousInstallDirectory, EnvironmentVariables).ToList();

			var selectedPlugins = installedPlugins ?? DefaultPlugins();

			foreach (var plugin in this.AvailablePlugins.Where(p => selectedPlugins.Contains(p.Url)))
				plugin.Selected = true;

			if (installedPlugins != null) InstalledPlugins.AddRange(installedPlugins);
			
			
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(this.GetType().Name);
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(Plugins)} = " + string.Join(", ", Plugins));
			sb.AppendLine($"- {nameof(InstalledPlugins)} = " + string.Join(", ", InstalledPlugins));
			return sb.ToString();
		}

		public virtual List<string> DefaultPlugins() => new List<string>();
	}
}