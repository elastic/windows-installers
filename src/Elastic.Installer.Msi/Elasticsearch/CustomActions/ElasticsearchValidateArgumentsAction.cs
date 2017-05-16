using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public class ElasticsearchValidateArgumentsAction : CustomAction<Elasticsearch>
	{
		public override string Name => nameof(ElasticsearchValidateArgumentsAction);
		public override int Order => (int)ElasticsearchCustomActionOrder.LogAllTheThings;
		public override Condition Condition => Condition.Always;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => Step.InstallInitialize;
		public override When When => When.After;
		public override Execute Execute => Execute.immediate;

		[CustomAction]
		public static ActionResult ElasticsearchValidateArguments(Session session) =>
			session.Handle(() => new ValidateArgumentsTask(session.ToSetupArguments(ElasticsearchArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}