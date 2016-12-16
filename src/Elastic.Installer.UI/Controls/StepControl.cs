using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FluentValidation.Results;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;
using Elastic.Installer.Domain.Model;

namespace Elastic.Installer.UI.Controls
{
	public abstract class StepControl<TViewModel, TControl> : UserControl, IViewFor<TViewModel>
		where TViewModel : ReactiveObject, IValidatableReactiveObject
		where TControl : StepControl<TViewModel, TControl>
	{
		object IViewFor.ViewModel
		{
			get { return ViewModel; }
			set { ViewModel = (TViewModel)value; }
		}
		
		public abstract TViewModel ViewModel { get; set; }

		private void InitializeGlobalStepBindings()
		{
			this.InitializeBindings();
			this.WhenAnyValue(view => view.ViewModel.ValidationFailures)
				.DistinctUntilChanged()
				.Subscribe(this._updateValidState);
		}

		protected abstract void InitializeBindings();

		protected virtual void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
		}

		protected void _updateValidState(IList<ValidationFailure> failures)
		{
			var isValid = !failures.Any();
			this.UpdateValidState(isValid, failures);
		}

		protected static void ViewModelPassed(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var self = d as TControl;
			self?.InitializeGlobalStepBindings();
		}
	}
}