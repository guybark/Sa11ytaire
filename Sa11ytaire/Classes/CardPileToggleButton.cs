// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Sol4All.Classes
{
    public class CardPileToggleButton : ToggleButton, INotifyPropertyChanged
    {
        private Card card;
        private Suit suit;

        public Card Card
        {
            get
            {
                return this.card;
            }
            set
            {
                this.card = value;
                this.OnPropertyChanged("Card");
                this.OnPropertyChanged("CardContent");
                this.OnPropertyChanged("CardPileAccessibleName");
                this.OnPropertyChanged("CardPileImage");
            }
        }

        public Suit Suit
        {
            get
            {
                return this.suit;
            }
            set
            {
                this.suit = value;
                this.OnPropertyChanged("Suit");
            }
        }

        public string CardPileAccessibleName
        {
            get
            {
                string cardPileAccessibleName;

                // Is this card pile empty?
                if (this.Card == null)
                {
                    // We'll load up a suit-specific localized string which indicates
                    // that no card is in this pile.
                    string suitResourceKey;

                    switch (this.Suit)
                    {
                        case Suit.Clubs:
                            suitResourceKey = "ClubsPile";
                            break;

                        case Suit.Diamonds:
                            suitResourceKey = "DiamondsPile";
                            break;

                        case Suit.Hearts:
                            suitResourceKey = "HeartsPile";
                            break;

                        default:
                            suitResourceKey = "SpadesPile";
                            break;
                    }

                    // Now get the localized string.
                    var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    cardPileAccessibleName = resourceLoader.GetString(suitResourceKey);
                }
                else
                {
                    // There is a card in this pile, so simply get the card's friendly name.
                    cardPileAccessibleName = this.Card.ToString();
                }

                return cardPileAccessibleName;
            }
        }

        public BitmapImage CardPileImage
        {
            get
            {
                string cardAsset;

                // Is this card pile empty?
                if (this.Card == null)
                {
                    switch (this.Suit)
                    {
                        case Suit.Clubs:
                            cardAsset = "EmptyTargetCardPileClubs";
                            break;

                        case Suit.Diamonds:
                            cardAsset = "EmptyTargetCardPileDiamonds";
                            break;

                        case Suit.Hearts:
                            cardAsset = "EmptyTargetCardPileHearts";
                            break;

                        default:
                            cardAsset = "EmptyTargetCardPileSpades";
                            break;
                    }
                }
                else
                {
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

                        default:
                            cardAsset = "Spades";
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

                        default:
                            cardAsset += "King";
                            break;
                    }
                }

                return new BitmapImage(new Uri("ms-appx:///Assets/Cards/" + cardAsset + ".png"));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
