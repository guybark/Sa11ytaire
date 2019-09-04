// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace Sol4All.Classes
{
    public class NextCardPileButton : Button, INotifyPropertyChanged
    {
        private bool isEmpty = false;

        public bool IsEmpty
        {
            get
            {
                return this.isEmpty;
            }
            set
            {
                this.isEmpty = value;

                this.OnPropertyChanged("IsEmpty");
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
    }

}
