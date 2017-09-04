using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.Installer.UI.Progress
{
	/// <summary>
	/// Maintains state about the progress of the installation
	/// </summary>
	/// <remarks>
	/// From https://github.com/wixtoolset/wix3/blob/a83b2130299ef95c9b0ec5c6d80bf34cc4722dfb/src/DTF/Samples/EmbeddedUI/InstallProgressCounter.cs
	/// </remarks>
	public class InstallProgressCounter
	{
		private static readonly Regex ActionTemplate = new Regex(@"\[(?<num>\d+)\]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private readonly double _scriptPhaseWeight;
		private int _total;
		private int _completed;
		private int _step;
		private bool _moveForward;
		private bool _enableActionData;
		private int _progressPhase;
		private string _lastMessage;
		private string _lastTemplate;

		public InstallProgressCounter() : this(0.3)
		{
		}

		public InstallProgressCounter(double scriptPhaseWeight)
		{
			if (!(0 <= scriptPhaseWeight && scriptPhaseWeight <= 1))
				throw new ArgumentOutOfRangeException(nameof(scriptPhaseWeight));

			this._scriptPhaseWeight = scriptPhaseWeight;
		}

		/// <summary>
		/// Gets a number between 0 and 1 that indicates the overall installation progress.
		/// </summary>
		public double Progress { get; private set; }

		public ProgressIndicator ProcessMessage(InstallMessage messageType, Record messageRecord)
		{
			switch (messageType)
			{
				case InstallMessage.ActionStart:
					if (this._enableActionData)
						this._enableActionData = false;

					// get the ActionStart message
					if (messageRecord.FieldCount > 1)
					{
						var message = messageRecord.GetString(2);
						if (!string.IsNullOrEmpty(message))
						{
							// Don't set (and therefore display) the last message if it has actiondata placeholders in it
							this._lastMessage = ActionTemplate.IsMatch(message) ? this._lastMessage : message;
							this._lastTemplate = message;
						}
					}

					return new ProgressIndicator(this.Progress, this._lastMessage);

				case InstallMessage.ActionData:
					if (this._enableActionData)
					{
						if (this._moveForward)
							this._completed += this._step;
						else
							this._completed -= this._step;
					}

					this.UpdateProgress();

					if (messageRecord.FieldCount > 0)
					{
						// template is prefixed with the action name in double delimited braces
						var actionData = Regex.Replace(messageRecord.GetString(0), "{{.*}}", string.Empty);
						actionData = !string.IsNullOrEmpty(actionData) ? actionData : _lastTemplate;
						if (!string.IsNullOrEmpty(actionData))
						{
							// Get the Actiondata fields and replace each placeholder in the ActionStart template
							// with the ActionData field value. The try/catch is needed here as the template may contain
							// a placeholder for an ActionData field value that does not exist.
							actionData = ActionTemplate.Replace(actionData, m =>
								{
									var number = int.Parse(m.Groups["num"].Value);
									try
									{
										var value = messageRecord[number];
										return value?.ToString() ?? string.Empty;
									}
									catch
									{
										return string.Empty;
									}
								});

							this._lastMessage = actionData;
						}
					}

					return new ProgressIndicator(this.Progress, this._lastMessage);

				case InstallMessage.Progress:
					return this.ProcessProgressMessage(messageRecord);

				default:
					return new ProgressIndicator(this.Progress, _lastMessage);
			}
		}

		/// <summary>
		/// Processes a Progress Message
		/// </summary>
		/// <remarks>See 
		/// https://msdn.microsoft.com/en-us/library/windows/desktop/aa370354(v=vs.85).aspx 
		/// for understanding the different types of progress record</remarks>
		/// <param name="progressRecord"></param>
		/// <returns></returns>
		private ProgressIndicator ProcessProgressMessage(Record progressRecord)
		{
			if (progressRecord == null || progressRecord.FieldCount == 0)
				return new ProgressIndicator(this.Progress, _lastMessage);

			var fieldCount = progressRecord.FieldCount;
			var progressType = progressRecord.GetInteger(1);

			switch (progressType)
			{
				case 0: // Master progress reset
					if (fieldCount < 4)
						return new ProgressIndicator(this.Progress, _lastMessage);

					this._progressPhase++;
					this._total = progressRecord.GetInteger(2);

					if (this._progressPhase == 1)
					{
						// HACK!!! this is a hack courtesy of the Windows Installer team. It seems the script planning phase
						// is always off by "about 50".  So we'll toss an extra 50 ticks on so that the standard progress
						// doesn't go over 100%.  If there are any custom actions, they may blow the total so we'll call this
						// "close" and deal with the rest.
						this._total += 50;
					}

					this._moveForward = progressRecord.GetInteger(3) == 0;
					// if forward, start at 0, if backwards start at max
					this._completed = this._moveForward ? 0 : this._total;
					this._enableActionData = false;

					this.UpdateProgress();

					return new ProgressIndicator(this.Progress, this._lastMessage);

				case 1: // Action info
					if (fieldCount < 3)
						return new ProgressIndicator(this.Progress, _lastMessage);

					if (progressRecord.GetInteger(3) == 0)
					{
						this._enableActionData = false;
					}
					else
					{
						this._enableActionData = true;
						this._step = progressRecord.GetInteger(2);
					}

					return new ProgressIndicator(this.Progress, this._lastMessage);

				case 2: // Progress report
					if (fieldCount < 2 || this._total == 0 || this._progressPhase == 0)
						return new ProgressIndicator(this.Progress, _lastMessage);

					if (this._moveForward)
						this._completed += progressRecord.GetInteger(2);
					else
						this._completed -= progressRecord.GetInteger(2);

					this.UpdateProgress();

					return new ProgressIndicator(this.Progress, this._lastMessage);

				case 3: // Progress total addition
					this._total += progressRecord.GetInteger(2);

					return new ProgressIndicator(this.Progress, this._lastMessage);

				default:
					return new ProgressIndicator(this.Progress, this._lastMessage);
			}
		}

		private void UpdateProgress()
		{
			if (this._progressPhase < 1 || this._total == 0)
			{
				this.Progress = 0;
			}
			else if (this._progressPhase == 1)
			{
				this.Progress = this._scriptPhaseWeight * Math.Min(this._completed, this._total) / this._total;
			}
			else if (this._progressPhase == 2)
			{
				this.Progress = this._scriptPhaseWeight +
					(1 - this._scriptPhaseWeight) * Math.Min(this._completed, this._total) / this._total;
			}
			else
			{
				this.Progress = this._scriptPhaseWeight +
					(1 - this._scriptPhaseWeight) * Math.Min(this._completed, this._total) / this._total;
			}
		}
	}
}