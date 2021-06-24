using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PathIconSource : IconSource
	{
		public PathIconSource() 
		{
		}

		public Geometry Data
		{
			get => (Geometry)GetValue(DataProperty);
			set => SetValue(DataProperty, value);
		}

		public static DependencyProperty DataProperty { get; } =
			DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIconSource), new PropertyMetadata(null, OnPropertyChanged));

		internal protected override IconElement CreateIconElementCore()
		{
			var pathIcon = new PathIcon();

			if (Data != null)
			{
				pathIcon.Data = Data;
			}

			if (Foreground != null)
			{
				pathIcon.Foreground = Foreground;
			}

			return pathIcon;
		}

		internal protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty sourceProperty)
		{
			if (sourceProperty == DataProperty)
			{
				return PathIcon.DataProperty;
			}

			return base.GetIconElementPropertyCore(sourceProperty);
		}
	}
}
