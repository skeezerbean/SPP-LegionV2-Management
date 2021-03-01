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

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			// for .NET Core you need to add UseShellExecute = true
			// see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
	}
}
