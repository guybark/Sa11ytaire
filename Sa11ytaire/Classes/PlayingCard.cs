// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Sol4All.Classes
{
    public class PlayingCard : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public static readonly DependencyProperty FaceDownProperty =
            DependencyProperty.Register(
            "FaceDown",
            typeof(bool),
            typeof(PlayingCard),
            null
        );

        public static readonly DependencyProperty CardStateProperty =
            DependencyProperty.Register(
            "CardState",
            typeof(CardState),
            typeof(PlayingCard),
            null
        );

        public static readonly DependencyProperty IsKingDropZoneProperty =
            DependencyProperty.Register(
            "IsKingDropZone",
            typeof(bool),
            typeof(PlayingCard),
            null
        );

        public static readonly DependencyProperty IsScannedProperty =
            DependencyProperty.Register(
            "IsScanned",
            typeof(bool),
            typeof(PlayingCard),
            null
        );

        public CardState CardState
        {
            get { return (CardState)GetValue(CardStateProperty); }
            set
            {
                SetValue(CardStateProperty, value);

                OnPropertyChanged("CardState");
                OnPropertyChanged("FaceDown");
                OnPropertyChanged("IsKingDropZone");
            }
        }

        public bool FaceDown
        {
            get { return (bool)GetValue(FaceDownProperty); }
            set
            {
                SetValue(FaceDownProperty, value);

                OnPropertyChanged("FaceDown");
                OnPropertyChanged("CardState");
                OnPropertyChanged("Name");
                OnPropertyChanged("Suit");
                OnPropertyChanged("Rank");

                // Barker: Raise the Card property changed event to force a
                // refresh of the image shown on the card. Consider how this
                // can be done without piggy-backing on the FaceDown setter.
                OnPropertyChanged("Card");
            }
        }

        public bool IsScanned
        {
            get { return (bool)GetValue(IsScannedProperty); }
            set
            {
                SetValue(IsScannedProperty, value);
                OnPropertyChanged("IsScanned");
            }
        }

        public bool IsKingDropZone
        {
            get { return (bool)GetValue(IsKingDropZoneProperty); }
            set
            {
                SetValue(IsKingDropZoneProperty, value);
                OnPropertyChanged("IsKingDropZone");
                OnPropertyChanged("CardState");
            }
        }

        private Card card;

        public Card Card { get => card; set => card = value; }

        public int InitialIndex { get; set; }

        public int ListIndex { get; set; }

        public Suit Suit { get => card.Suit; }

        public int Rank { get => card.Rank; }

        public string Name
        {
            get
            {
                string name;

                var resourceLoader = new ResourceLoader();

                if (this.FaceDown)
                {
                    name =  resourceLoader.GetString("FaceDown") + " " +
                                resourceLoader.GetString(this.InitialIndex.ToString());
                }
                else if (this.card.Rank != 0)
                {
                    name = this.card.ToString();
                }
                else
                {
                    name = resourceLoader.GetString("Empty") + " " +
                        resourceLoader.GetString(this.ListIndex.ToString());
                }

                return name;
            }
        }

        public void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}
