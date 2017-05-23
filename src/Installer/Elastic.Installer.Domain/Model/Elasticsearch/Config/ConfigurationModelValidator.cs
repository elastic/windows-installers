using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Config
{
	public class ConfigurationModelValidator : AbstractValidator<ConfigurationModel>
	{
		public static readonly string NodeNameNotEmpty = TextResources.ConfigurationModelValidator_NodeName_NotEmpty; 
		public static readonly string ClusterNameNotEmpty = TextResources.ConfigurationModelValidator_ClusterName_NotEmpty; 
		public static readonly string SelectedMemoryGreaterThanOrEqual250Mb = TextResources.ConfigurationModelValidator_SelectedMemory_GreaterThanOrEqual250MB; 
		public static readonly string SelectedMemoryLessThan32Gb = TextResources.ConfigurationModelValidator_SelectedMemory_LessThan32GB; 
		public static readonly string MaxMemory50Percent = TextResources.ConfigurationModelValidator_MaxMemory_50Percent; 
		public static readonly string NegativeMinimumMasterNodes = TextResources.ConfigurationModelValidator_NegativeMinimumMasterNodes; 
		public static readonly string HttpPortMinimum = TextResources.ConfigurationModelValidator_HttpPortMinimum; 
		public static readonly string TransportPortMinimum = TextResources.ConfigurationModelValidator_TransportPortMinimum; 
		public static readonly string PortMaximum = TextResources.ConfigurationModelValidator_PortMaximum; 
		public static readonly string EqualPorts = TextResources.ConfigurationModelValidator_EqualPorts; 

		public ConfigurationModelValidator ()
		{
			RuleFor(c => c.NodeName)
				.NotEmpty().WithMessage(NodeNameNotEmpty);
			RuleFor(c => c.ClusterName)
				.NotEmpty().WithMessage(ClusterNameNotEmpty);
			RuleFor(c => c.SelectedMemory)
				.Must((m,s)=> s >= m.MinSelectedMemory).WithMessage(SelectedMemoryGreaterThanOrEqual250Mb);
			RuleFor(c => c.SelectedMemory)
				.Must((m,s)=> s < ConfigurationModel.CompressedOrdinaryPointersThreshold).WithMessage(SelectedMemoryLessThan32Gb);
			RuleFor(c => c.SelectedMemory)
				.Must((m,s)=> s <= (m.TotalPhysicalMemory / 2)).WithMessage(MaxMemory50Percent);

			RuleFor(c => c.MinimumMasterNodes)
				.Must((m,s)=> s >= 0).WithMessage(NegativeMinimumMasterNodes);

			RuleFor(c => c.HttpPort)
				.Must((m, s) => (s.HasValue && s >= ConfigurationModel.HttpPortMinimum) || !s.HasValue)
				.WithMessage(HttpPortMinimum, ConfigurationModel.HttpPortMinimum)
				.Must((m, s) => (s.HasValue && s <= ConfigurationModel.PortMaximum) || !s.HasValue)
				.WithMessage(PortMaximum, ConfigurationModel.PortMaximum)
				.Must((m,s) => (s.HasValue && m.TransportPort.HasValue && (s != m.TransportPort)) || !s.HasValue)
				.WithMessage(EqualPorts, m=>m.HttpPort);

			RuleFor(c => c.TransportPort)
				.Must((m,s)=> (s.HasValue && s >= ConfigurationModel.TransportPortMinimum) || !s.HasValue)
				.WithMessage(TransportPortMinimum, ConfigurationModel.TransportPortMinimum)
				.Must((m,s)=> (s.HasValue && s <= ConfigurationModel.PortMaximum) || !s.HasValue)
				.WithMessage(PortMaximum, ConfigurationModel.PortMaximum);
		}
	}
}
