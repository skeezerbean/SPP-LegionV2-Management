using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SPP_LegionV2_Management
{
	/// <summary>
	/// Interaction logic for ShellView.xaml
	/// </summary>
	public partial class ShellView : MetroWindow
	{
		public ShellView()
		{
			InitializeComponent();
			ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(System.Int32.MaxValue));
		}

		private void HelpAbout(object sender, RoutedEventArgs e)
		{
			// for .NET Core you need to add UseShellExecute = true
			// see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
			Process.Start(new ProcessStartInfo("https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/README.md"));
			e.Handled = true;
		}
	}
}