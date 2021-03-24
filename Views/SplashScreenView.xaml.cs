using System.Windows;

namespace SPP_LegionV2_Management
{
	/// <summary>
	/// Interaction logic for SplashScreen.xaml
	/// </summary>
	public partial class SplashScreenView : Window
	{
		// Graphics for splash screen designed by m1#2698 and are pretty awesome!
		public SplashScreenView()
		{
			InitializeComponent();
		}

		private void Storyboard_Completed(object sender, System.EventArgs e)
		{
			this.Close();
		}
	}
}
