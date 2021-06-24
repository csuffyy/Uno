using System;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BitmapIconSource : IconSource
	{
		public BitmapIconSource()
		{
		}

		public Uri UriSource
		{
			get => (Uri)GetValue(UriSourceProperty);
			set => SetValue(UriSourceProperty, value);
		}

		public static DependencyProperty UriSourceProperty { get; } =
			DependencyProperty.Register(nameof(UriSource), typeof(Uri), typeof(BitmapIconSource), new PropertyMetadata(default(Uri), OnPropertyChanged));

		public bool ShowAsMonochrome
		{
			get => (bool)GetValue(ShowAsMonochromeProperty);
			set => SetValue(ShowAsMonochromeProperty, value);
		}

		public static DependencyProperty ShowAsMonochromeProperty { get; } =
			DependencyProperty.Register(nameof(ShowAsMonochrome), typeof(bool), typeof(BitmapIconSource), new PropertyMetadata(default(bool), OnPropertyChanged));

		internal protected override IconElement CreateIconElementCore()
		{
			var bitmapIcon = new BitmapIcon();

			if (UriSource != null)
			{
				bitmapIcon.UriSource = UriSource;
			}

			if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.BitmapIcon", "ShowAsMonochrome"))
			{
				bitmapIcon.ShowAsMonochrome = ShowAsMonochrome;
			}

			if (Foreground != null)
			{
				bitmapIcon.Foreground = Foreground;
			}

			return bitmapIcon;
		}

		internal protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty sourceProperty)
		{
			if (sourceProperty == ShowAsMonochromeProperty)
			{
				return BitmapIcon.ShowAsMonochromeProperty;
			}
			else if (sourceProperty == UriSourceProperty)
			{
				return BitmapIcon.UriSourceProperty;
			}

			return base.GetIconElementPropertyCore(sourceProperty);
		}
	}
}
