using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SPP_LegionV2_Management
{
	/// <summary>
	/// Interaction logic for ShellView.xaml
	/// </summary>
	public partial class ShellView : Window
	{
		public ShellView()
		{
			InitializeComponent();
			ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(System.Int32.MaxValue));
			SplashScreenView ss = new SplashScreenView();
			Console.WriteLine("Starting Show");
			ss.Show();
			Console.WriteLine("Starting sleep");
			//Thread.Sleep(3000);
			//Console.WriteLine("Sleep done");
			//ss.Close();
			//ss.Close();
			//Console.WriteLine("Close() done");
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