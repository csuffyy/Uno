﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Views;
using Java.Lang;
using Uno.Extensions;
using Uno.UI.Controls;
using Uno.Extensions.Specialized;
using AndroidX.ViewPager.Widget;

namespace Windows.UI.Xaml.Controls
{
	public class FlipViewAdapter : PagerAdapter
	{
		private WeakReference<FlipView> _ownerReference;
		/// <summary>
		/// The FlipView which uses this adapter. This property is backed by a weak reference.
		/// </summary>
		internal FlipView Owner
		{
			get { return _ownerReference?.GetTarget(); }
			set { _ownerReference = new WeakReference<FlipView>(value); }
		}
		public override int Count
		{
			get
			{
				return Owner?.GetItems()?.Count() ?? 0;
			}
		}

		public override bool IsViewFromObject(View view, Java.Lang.Object objectValue)
		{
			return view == objectValue;
		}

		public override Java.Lang.Object InstantiateItem(ViewGroup containerWrapper, int position)
		{
			var owner = Owner;

			var container = owner.GetContainerForIndex(position) as UIElement;

			owner.PrepareContainerForIndex(container, position);

			containerWrapper.AddView(container);

			return container;
		}

		public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object objectValue)
		{
			//If container's content is a view, unset it so that it will be restored properly when the item is shown again.
			var itemView = objectValue as FlipViewItem;
			if (itemView != null &&
				itemView.Content is View &&
				(!Owner?.IsItemItsOwnContainer(itemView) ?? false))
			{
				itemView.Content = null;
			}

			container.RemoveView((View)objectValue);

			if (itemView?.ChildCount > 0 && itemView.IsGeneratedContainer)
			{
				// Reset the DataContext from the item, which may have been set by the FlipView. 
				// Since we're manually removing the content, the DataContext may not be reset by 
				// the parent FlipViewItem and will be recycled incorrectly if the DataContext is set.
				(itemView.TemplatedRoot as DependencyObject)?.ClearValue(FrameworkElement.DataContextProperty);

				//The FlipView doesn't recycle its items on Android, but removing the child view will free up its template to be reused by 
				// Uno's template pooling. (We don't do this for FlipViewItems explicitly defined in xaml, only for ones generated by us.)
				itemView.RemoveViewAt(0);
			}
		}

		public override int GetItemPosition(Java.Lang.Object objectKey)
		{
			var itemView = objectKey as FlipViewItem;

			//We return PositionNone instead of the default PositionUnchanged 
			//to force the ViewPager to remove all children if the item cannot be resolved in the current ItemsSource
			if (itemView == null)
			{
				return PositionNone;
			}
			else
			{
				var index = Owner.GetItems()?.IndexOf(itemView.Content);

				return index >= 0 ? index.Value : PositionNone;
			}
		}
	}
}
