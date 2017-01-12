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

		//ReactiveUI 7 will allow creating these from lambda's on Bind() (rejoice!)
		protected class IntToDoubleConverter : IBindingTypeConverter
		{
			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				result = 0.0;
				if (@from == null) return true;
				if (@from is int)
				{
					result = (double)((int)@from);
					return true;
				}
				if (!(@from is double)) return true;
				result = ((double)@from);
				return true;
			}
		}
		protected class NullableDoubleToIntConverter : IBindingTypeConverter
		{
			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				result = 0;
				if (!(@from is double)) return true;
				result = (int)((double)@from);
				return true;
			}
		}
		protected class NullableDoubleToNullableIntConverter : IBindingTypeConverter
		{
			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				if (@from == null)
				{
					result = null;
					return true;
				}
				if (@from is int)
				{
					result = (int) @from;
					return true;
				}
				if (@from is double)
				{
					result = (int)((double) @from);
					return true;
				}
				result = null;
				return true;
			}
		}
		protected class NullableIntToNullableDoubleConverter : IBindingTypeConverter
		{
			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				if (@from == null)
				{
					result = null;
					return true;
				}
				if (@from is int)
				{
					result = Convert.ToDouble((int)@from);
					return true;
				}
				result = null;
				return true;
			}
		}
	}
}