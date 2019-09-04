// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace Sol4All.Classes
{
    // CardListView only exists to disable card items that are face-down in the list.
    public class CardListView : ListView
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            PlayingCard card = item as PlayingCard;
            if (card != null)
            {
                ListViewItem lvItem = element as ListViewItem;
                if (lvItem != null)
                {
                    lvItem.VerticalContentAlignment = VerticalAlignment.Stretch;

                    // Set up binding, such that the ListViewIems's IsEnabled is 
                    // bound be the opposite of the FaceDown property.
                    Binding binding = new Binding();
                    binding.Mode = BindingMode.OneWay;
                    binding.Source = card;
                    binding.Path = new PropertyPath("FaceDown");
                    binding.Converter = new FaceDownToFaceUpVisibilityConverter();
                    lvItem.SetBinding(ListViewItem.IsEnabledProperty, binding);
                }
            }
        }
    }
}
