using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.IO;
using WixSharp;

namespace Elastic.Installer.Msi.CustomActions
{
	public abstract class UninstallCustomAction<TProduct> : CustomAction<TProduct>
		where TProduct : Product, new()
	{
		public override Condition Condition => Condition.Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Execute Execute => Execute.deferred;
	}
}
