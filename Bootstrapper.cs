using Caliburn.Micro;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SPP_LegionV2_Management
{
	public class Bootstrapper : BootstrapperBase
	{
		private DateTime time = DateTime.Now;

		public Bootstrapper()
		{
			Initialize();
		}

		protected override async void OnStartup(object sender, StartupEventArgs e)
		{
			SplashScreenView ss = new SplashScreenView();

			// Show our splash screen, wait to fade before popping up main window
			ss.Show();
			await Task.Delay(1000);
			while (ss.Opacity > 0) { await Task.Delay(1); }

			// Pop up our main view, and close splash screen
			DisplayRootViewFor<ShellViewModel>();
			ss.Close();
		}
	}
}