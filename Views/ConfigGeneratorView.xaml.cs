using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SPP_LegionV2_Management
{

	/// <summary>
	/// Interaction logic for ConfigGeneratorView.xaml
	/// </summary>
	public partial class ConfigGeneratorView : UserControl
	{
		// Here we create the viewmodel with the current DialogCoordinator instance
		private ConfigGeneratorViewModel vm = new ConfigGeneratorViewModel(DialogCoordinator.Instance);

		public ConfigGeneratorView()
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
