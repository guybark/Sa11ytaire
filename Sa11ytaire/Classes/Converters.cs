// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Sol4All.Classes
{
    public class IsFaceDownToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isFaceDown = (bool)value;

            return (isFaceDown ? Visibility.Collapsed : Visibility.Visible);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class FaceDownToFaceUpVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isFaceDown = (bool)value;

            return !isFaceDown;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class UpturnedCardToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value == null ? Visibility.Collapsed : Visibility.Visible);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NextCardIsEmptyToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isEmpty = (bool)value;

            string cardAsset = isEmpty ? "EmptyDealtCardPile" : "CardBack";

            return new BitmapImage(new Uri("ms-appx:///Assets/Cards/" + cardAsset + ".png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NextCardIsEmptyToAccessibleName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isEmpty = (bool)value;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            return resourceLoader.GetString(
                isEmpty ? "NextCardPile_TurnOverCards" : "NextCardPile_NextCard");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IsCardStateToCardBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            CardState state = (CardState)value;

            SolidColorBrush backgroundBrush;

            switch (state)
            {
                case CardState.FaceDown:
                    backgroundBrush = Application.Current.Resources["PlayingCardBackFaceDownBackgroundBrush"] as SolidColorBrush;
                    break;

                default:
                    backgroundBrush = null;
                    break;
            }

            return backgroundBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IsScannedToCardBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isScanned = (bool)value;

            SolidColorBrush borderBrush;

            if (isScanned)
            {
                borderBrush = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;
            }
            else
            {
                borderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
            }

            return borderBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IsScannedToCardBorderThicknessConverter : IValueConverter
    {
        Thickness _thicknessDefault = new Thickness(1);
        Thickness _thicknessScanned = new Thickness(4);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isScanned = (bool)value;

            return (isScanned ? _thicknessScanned : _thicknessDefault);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class CardToCardImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Card card = (Card)value;

            string cardAsset;

            switch (card.Suit)
            {
                case Suit.Clubs:
                    cardAsset = "Clubs";
                    break;

                case Suit.Diamonds:
                    cardAsset = "Diamonds";
                    break;

                case Suit.Hearts:
                    cardAsset = "Hearts";
                    break;

                case Suit.Spades:
                    cardAsset = "Spades";
                    break;

                default:
                    cardAsset = "";
                    break;
            }

            switch (card.Rank)
            {
                case 1:
                    cardAsset += "Ace";
                    break;

                case 2:
                    cardAsset += "Two";
                    break;

                case 3:
                    cardAsset += "Three";
                    break;

                case 4:
                    cardAsset += "Four";
                    break;

                case 5:
                    cardAsset += "Five";
                    break;

                case 6:
                    cardAsset += "Six";
                    break;

                case 7:
                    cardAsset += "Seven";
                    break;

                case 8:
                    cardAsset += "Eight";
                    break;

                case 9:
                    cardAsset += "Nine";
                    break;

                case 10:
                    cardAsset += "Ten";
                    break;

                case 11:
                    cardAsset += "Jack";
                    break;

                case 12:
                    cardAsset += "Queen";
                    break;

                case 13:
                    cardAsset += "King";
                    break;

                default:
                    cardAsset = "EmptyDealtCardPile";
                    break;
            }

            if (string.IsNullOrEmpty(cardAsset))
            {
                return null;
            }

            return new BitmapImage(new Uri("ms-appx:///Assets/Cards/" + cardAsset + ".png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IsCheckedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = (bool)value;

            return (isChecked ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}