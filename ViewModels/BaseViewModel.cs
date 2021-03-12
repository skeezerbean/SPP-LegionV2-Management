using Caliburn.Micro;
using PropertyChanged;

namespace SPP_LegionV2_Management
{
	// This fires off property event changes as needed
	[AddINotifyPropertyChangedInterface]
	// Conductor from Caliburn - enables 1 child in the view at a time
	public class BaseViewModel : Conductor<object>.Collection.OneActive
	{
	}
}