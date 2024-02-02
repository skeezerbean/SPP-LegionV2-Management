using System.Windows;
using System.Windows.Media;

namespace SPP_LegionV2_Management
{
	public class EyeCandyForeGround
	{
		#region Image dependency property

		/// <summary>
		/// An attached dependency property which provides an
		/// <see cref="ImageSource" /> for arbitrary WPF elements.
		/// </summary>
		public static readonly DependencyProperty ImageProperty;

		/// <summary>
		/// Gets the <see cref="ImageProperty"/> for a given
		/// <see cref="DependencyObject"/>, which provides an
		/// <see cref="ImageSource" /> for arbitrary WPF elements.
		/// </summary>
		public static ImageSource GetImage(DependencyObject obj)
		{
			return (ImageSource)obj.GetValue(ImageProperty);
		}

		/// <summary>
		/// Sets the attached <see cref="ImageProperty"/> for a given
		/// <see cref="DependencyObject"/>, which provides an
		/// <see cref="ImageSource" /> for arbitrary WPF elements.
		/// </summary>
		public static void SetImage(DependencyObject obj, ImageSource value)
		{
			obj.SetValue(ImageProperty, value);
		}

		#endregion Image dependency property

		static EyeCandyForeGround()
		{
			//register attached dependency property
			var metadata = new FrameworkPropertyMetadata((ImageSource)null);
			ImageProperty = DependencyProperty.RegisterAttached("Image",
																typeof(ImageSource),
																typeof(EyeCandyForeGround), metadata);
		}
	}

	public class EyeCandyBackGround
	{
		#region Image dependency property

		/// <summary>
		/// An attached dependency property which provides an
		/// <see cref="ImageSource" /> for arbitrary WPF elements.
		/// </summary>
		public static readonly DependencyProperty ImageProperty;

		/// <summary>
		/// Gets the <see cref="ImageProperty"/> for a given
		/// <see cref="DependencyObject"/>, which provides an
		/// <see cref="ImageSource" /> for arbitrary WPF elements.
		/// </summary>
		public static ImageSource GetImageBackGround(DependencyObject obj)
		{
			return (ImageSource)obj.GetValue(ImageProperty);
		}

		/// <summary>
		/// Sets the attached <see cref="ImageProperty"/> for a given
		/// <see cref="DependencyObject"/>, which provides an
		/// <see cref="ImageSource" /> for arbitrary WPF elements.
		/// </summary>
		public static void SetImageBackGround(DependencyObject obj, ImageSource value)
		{
			obj.SetValue(ImageProperty, value);
		}

		#endregion Image dependency property

		static EyeCandyBackGround()
		{
			//register attached dependency property
			var metadata = new FrameworkPropertyMetadata((ImageSource)null);
			ImageProperty = DependencyProperty.RegisterAttached("ImageBackGround",
																typeof(ImageSource),
																typeof(EyeCandyBackGround), metadata);
		}
	}
}