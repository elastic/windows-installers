using WixSharp;

namespace Elastic.Installer.Msi.CustomActions
{
	public abstract class CommitCustomAction<TProduct> : CustomAction<TProduct>
		where TProduct : Product, new()
	{
		public override Execute Execute => Execute.commit;
		public override Condition Condition => null;
		public override Return Return => Return.ignore;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.Before;
		public override Step Step => Step.InstallFinalize;
	}
}