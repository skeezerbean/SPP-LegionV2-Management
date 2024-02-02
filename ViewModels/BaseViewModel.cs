using Caliburn.Micro;
using PropertyChanged;

namespace SPP_LegionV2_Management
{
	// This fires off property event changes as needed
	[AddINotifyPropertyChangedInterface]
	public class BaseViewModel : Conductor<object>.Collection.OneActive
	{
	}
}