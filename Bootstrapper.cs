using Caliburn.Micro;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SPP_LegionV2_Management
{
	public class Bootstrapper : BootstrapperBase
	{
		DateTime time = DateTime.Now;
		public Bootstrapper()
		{
			Initialize();
		}

		protected override async void OnStartup(object sender, StartupEventArgs e)
		{
			SplashScreenView ss = new SplashScreenView();

			// Show our splash screen, wait a bit before popping up main window
			ss.Show();
			while (time.AddMilliseconds(4300) > DateTime.Now) { await Task.Delay(1); }

			DisplayRootViewFor<ShellViewModel>();
		}
	}
}