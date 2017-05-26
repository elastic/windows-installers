using WixSharp;

namespace Elastic.Installer.Msi.CustomActions
{
	public abstract class RollbackCustomAction<TProduct> : CustomAction<TProduct> 
		where TProduct : Product, new()
	{
		public override Execute Execute => Execute.rollback;
		public override Condition Condition => null;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.Before;
	}
}
