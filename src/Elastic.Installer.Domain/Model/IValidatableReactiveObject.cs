using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Results;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model
{

	public interface IValidatableReactiveObject
	{

		/// <summary>The model validates and can be considered being in a valid state</summary>
		bool IsValid { get; }

		/// <summary>The model's validation failures reflecting the state errors</summary>
		IList<ValidationFailure> ValidationFailures { get; }

		/// <summary>Force a model to refresh to its initial (not necessarily valid) state</summary>
		void Refresh();

		/// <summary>Force a validation of the model</summary>
		void Validate();

	}

	public abstract class ValidatableReactiveObjectBase<TModel, TModelValidator> : ReactiveObject, IValidatableReactiveObject
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		protected TModelValidator Validator { get; }

		public abstract void Refresh();
		public void Validate()
		{
			var errors = this.Validator.Validate((TModel)this);
			this.IsValid = errors.IsValid;
			this.ValidationFailures = errors.Errors;
		}

		protected ValidatableReactiveObjectBase()
		{
			this.IsValid = true;
			this.Validator = new TModelValidator();
			this.Changed.Subscribe(x =>
			{
				if (x.PropertyName == nameof(ValidationFailures)) return;
				if (!this.SkipValidationFor(x.PropertyName))
					this.Validate();
			});
			this.Validate();
		}

		protected virtual bool SkipValidationFor(string propertyName) => false;

		// ReSharper disable InconsistentNaming (ReactiveUI convention)
		// ReSharper disable ArrangeTypeMemberModifiers
		bool isValid;
		public bool IsValid
		{
			get { return isValid; }
			private set { this.RaiseAndSetIfChanged(ref isValid, value); }
		}

		private IList<ValidationFailure> validationFailures = new List<ValidationFailure>();
		public IList<ValidationFailure> ValidationFailures
		{
			get { return validationFailures; }
			private set { this.RaiseAndSetIfChanged(ref validationFailures, value); }
		}

	}
}