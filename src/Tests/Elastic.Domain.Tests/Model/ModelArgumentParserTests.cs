using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Base;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Model
{
	public class ModelArgumentParserTests 
	{
		private class ModelA : IValidatableReactiveObject
		{
			public bool IsValid => true;
			public bool IsRelevant => true;
			public string[] HiddenProperties { get; set; }
			public void Refresh() { }
			public void Validate() { }
			public IList<ValidationFailure> ValidationFailures => null;
			public IList<ValidationFailure> PrerequisiteFailures => null;

			[Argument("A")]
			public string X { get; set; }
		}

		private class ModelB : IValidatableReactiveObject
		{
			public bool IsValid => true;
			public bool IsRelevant => true;
			public string[] HiddenProperties { get; set; }
			public void Refresh() { }
			public void Validate() { }
			public IList<ValidationFailure> ValidationFailures => null;
			public IList<ValidationFailure> PrerequisiteFailures => null;

			[Argument("A")]
			public string X { get; set; }
		}

		[Fact]
		public void NoValueIsEmptyString()
		{
			var args = new[]
			{
				$"X="
			};

			var viewModelArgumentParser = new ModelArgumentParser(new IValidatableReactiveObject[] { new ModelA() }, args);
			viewModelArgumentParser.ValidationFailures.Should().BeEmpty();
			viewModelArgumentParser.ViewModelArguments.Should().HaveCount(1);
			viewModelArgumentParser.ViewModelArguments.First().Value.Should().BeEmpty();
		}

		[Fact] public void KnownPropertyIsParsed()
		{
			var args = new[]
			{
				$"X=C:\\ESCONFIG"
			};

			var viewModelArgumentParser = new ModelArgumentParser(new IValidatableReactiveObject[] { new ModelA() }, args);
			viewModelArgumentParser.ValidationFailures.Should().BeEmpty();
			viewModelArgumentParser.ViewModelArguments.Should().HaveCount(1);
		}

		[Fact] public void EqualsInValueIsNotLost()
		{
			var args = new[]
			{
				$"X=C:\\ESCONFIG="
			};

			var viewModelArgumentParser = new ModelArgumentParser(new IValidatableReactiveObject[] { new ModelA() }, args);
			viewModelArgumentParser.ValidationFailures.Should().BeEmpty();
			viewModelArgumentParser.ViewModelArguments.Should().HaveCount(1);
			var viewModelArg = viewModelArgumentParser.ViewModelArguments.First();
			viewModelArg.Key.Should().Be("X");
			viewModelArg.Value.Should().Be("C:\\ESCONFIG=");
		}

		[Fact] public void UnknownPropertyIsAValidationFailure()
		{
			var args = new[]
			{
				$"Y=C:\\ESCONFIG"
			};

			var viewModelArgumentParser = new ModelArgumentParser(new IValidatableReactiveObject[] { new ModelA() }, args);
			viewModelArgumentParser.ValidationFailures.Should().NotBeEmpty().And.HaveCount(1);
			viewModelArgumentParser.ViewModelArguments.Should().BeEmpty();
		}

		[Fact] public void SpecifyingTheSameOptionTwiceShouldBeAValidationFailure()
		{
			var args = new[]
			{
				$"X=C:\\ESCONFIG",
				$"X=C:\\ESCONFIG"
			};

			var viewModelArgumentParser = new ModelArgumentParser(new IValidatableReactiveObject[] { new ModelA() }, args);
			viewModelArgumentParser.ValidationFailures.Should().NotBeEmpty().And.HaveCount(1);
			viewModelArgumentParser.ViewModelArguments.Should().NotBeEmpty().And.HaveCount(1);
		}

		[Fact] public void ParsingThrowsIfTwoViewModelsDefineSameVariable()
		{
			var args = new[] { $"X=2" };

			Action viewModelArgumentParser = () => new ModelArgumentParser(new IValidatableReactiveObject[] { new ModelA(), new ModelB() }, args);
			var exception = viewModelArgumentParser.ShouldThrow<ArgumentException>()
				.WithMessage("X can not be reused as argument option on ModelB as it already exists as a property on another model");
		}
	}
}
