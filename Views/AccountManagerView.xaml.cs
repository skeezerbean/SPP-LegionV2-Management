using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls;

namespace SPP_LegionV2_Management
{

	/// <summary>
	/// Interaction logic for AccountManagerView.xaml
	/// </summary>
	public partial class AccountManagerView : UserControl
	{
		// Here we create the viewmodel with the current DialogCoordinator instance
		private AccountManagerViewModel vm = new AccountManagerViewModel(DialogCoordinator.Instance);

		public AccountManagerView()
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
