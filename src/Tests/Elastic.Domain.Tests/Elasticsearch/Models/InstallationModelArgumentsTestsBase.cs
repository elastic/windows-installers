using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Elasticsearch;
using FluentAssertions;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public class InstallationModelArgumentsTestsBase : InstallationModelTestBase
	{
		protected void Argument<T>(string key, T value, Action<ElasticsearchInstallationModel> assert) =>
			this.Argument(key, value, (s, v) => assert(s));

		protected void Argument<T>(string key, T value, Action<ElasticsearchInstallationModel, T> assert)
		{
			var model = WithValidPreflightChecks().InstallationModel;
			string msiParams = AssertParser(model, key, value, assert);
			var msiParamString = model.ParsedArguments.MsiString(value);

			msiParams.Should().Contain($"{key.ToUpperInvariant()}=\"{msiParamString}\"");
		}

		protected void Argument<T>(string key, T value, string msiParam, Action<ElasticsearchInstallationModel, T> assert)
		{
			var model = WithValidPreflightChecks().InstallationModel;
			string msiParams = AssertParser(model, key, value, assert);
			msiParams.Should().Contain($"{key.ToUpperInvariant()}=\"{msiParam}\"");
		}

		private string AssertParser<T>(ElasticsearchInstallationModel model, string key, T value,
			Action<ElasticsearchInstallationModel, T> assert)
		{
			var args = new[] {$"{key.Split('.').Last()}={value}"};
			var models = model.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] {model}).ToList();
			var viewModelArgumentParser = new ElasticsearchArgumentParser(models, args);
			assert(model, value);

			var msiParams = model.ToMsiParamsString();
			msiParams.Should().NotBeEmpty();
			return msiParams;
		}

		protected MultipleArgumentsTester Argument<T>(string key, T value)
		{
			var model = WithValidPreflightChecks().InstallationModel;
			return new MultipleArgumentsTester(model).Argument(key, value);
		}

		protected MultipleArgumentsTester Argument<T>(string key, T value, string msiParam)
		{
			var model = WithValidPreflightChecks().InstallationModel;
			return new MultipleArgumentsTester(model).Argument(key, value, msiParam);
		}

		public class MultipleArgumentsTester
		{
			private readonly ElasticsearchInstallationModel _model;
			private List<Tuple<string, string, string>> Arguments { get; } = new List<Tuple<string, string, string>>();

			public MultipleArgumentsTester(ElasticsearchInstallationModel model)
			{
				this._model = model;
			}

			public MultipleArgumentsTester Argument<T>(string key, T value)
			{
				this.Arguments.Add(Tuple.Create(key, value.ToString(), value.ToString()));
				return this;
			}

			public MultipleArgumentsTester Argument<T>(string key, T value, string msiParam)
			{
				this.Arguments.Add(Tuple.Create(key, value.ToString(), msiParam));
				return this;
			}

			public string Assert(Action<ElasticsearchInstallationModel> assert)
			{
				var args = this.Arguments.Select(t => $"{t.Item1.Split('.').Last()}={t.Item3}").ToArray();
				var models = this._model.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] {this._model}).ToList();
				var viewModelArgumentParser = new ElasticsearchArgumentParser(models, args);
				assert(this._model);

				var msiParams = this._model.ToMsiParamsString();
				msiParams.Should().NotBeEmpty();
				foreach(var t in this.Arguments)
					msiParams.Should().Contain($"{t.Item1.ToUpperInvariant()}=\"{t.Item3}\"");
				return msiParams;
			}
		}
	}
}