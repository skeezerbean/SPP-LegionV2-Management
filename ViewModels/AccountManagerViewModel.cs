using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;

namespace SPP_LegionV2_Management
{
	public class AccountManagerViewModel : Screen
	{
		// IDialogCoordinator is for metro message boxes
		private readonly IDialogCoordinator _dialogCoordinator;

		// IDialogCoordinator is part of Metro, for dialog handling in the view model
		public AccountManagerViewModel(IDialogCoordinator instance)
		{
			_dialogCoordinator = instance;
		}
	}
}
