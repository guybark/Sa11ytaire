// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Sol4All.Classes;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Sol4All.ViewModels
{
    public class PlayingCardViewModel : INotifyPropertyChanged
    {
        private bool _scanModeOn;

        public bool ScanModeOn
        {
            get
            {
                return _scanModeOn;
            }
            set
            {
                _scanModeOn = value;
                OnPropertyChanged("ScanModeOn");
            }
        }

        private bool _singleKeyToMove = false;

        public bool SingleKeyToMove
        {
            get
            {
                return _singleKeyToMove;
            }
            set
            {
                _singleKeyToMove = value;
                OnPropertyChanged("SingleKeyToMove");
            }
        }

        private bool _selectWithoutAltKey = false;

        public bool SelectWithoutAltKey
        {
            get
            {
                return _selectWithoutAltKey;
            }
            set
            {
                _selectWithoutAltKey = value;
                OnPropertyChanged("SelectWithoutAltKey");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<PlayingCard> PlayingCards1 { get => playingCards[0]; }
        public ObservableCollection<PlayingCard> PlayingCards2 { get => playingCards[1]; }
        public ObservableCollection<PlayingCard> PlayingCards3 { get => playingCards[2]; }
        public ObservableCollection<PlayingCard> PlayingCards4 { get => playingCards[3]; }
        public ObservableCollection<PlayingCard> PlayingCards5 { get => playingCards[4]; }
        public ObservableCollection<PlayingCard> PlayingCards6 { get => playingCards[5]; }
        public ObservableCollection<PlayingCard> PlayingCards7 { get => playingCards[6]; }

        private ObservableCollection<PlayingCard>[] playingCards;

        public ObservableCollection<PlayingCard>[] PlayingCards { get => playingCards; set => playingCards = value; }

        public PlayingCardViewModel()
        {
            PlayingCards = new ObservableCollection<PlayingCard>[7];

            for (int i = 0; i < PlayingCards.Count(); ++i)
            {
                PlayingCards[i] = new ObservableCollection<PlayingCard>();
            }

        }

        private void PlayingCards1_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
