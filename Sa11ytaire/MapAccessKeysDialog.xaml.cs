// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Note: We're not selecting the TextBox text when tabbing into them. We could 
// do this, but there doesn't seem to be a property to set to make that happen.

namespace Sol4All
{
    public class AutoSelectTextBox : TextBox
    {
        public AutoSelectTextBox()
        {
            this.GotFocus += AutoSelectTextBox_GotFocus;
        }

        private void AutoSelectTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.SelectAll();
        }
    }

    public sealed partial class MapAccessKeysDialog : ContentDialog
    {
        private ApplicationDataContainer localSettings;

        public MapAccessKeysDialog(ApplicationDataContainer localSettings)
        {
            this.localSettings = localSettings;

            this.InitializeComponent();

            var resourceLoader = new ResourceLoader();
            this.Title = resourceLoader.GetString("MapAccessKeysTitle");
            this.PrimaryButtonText = resourceLoader.GetString("MapAccessKeysPrimaryButtonText");
            this.SecondaryButtonText = resourceLoader.GetString("MapAccessKeysSecondaryButtonText");

            this.DefaultButton = ContentDialogButton.Primary;

            string accessKey = (string)localSettings.Values["AccessKeyN"];
            if (accessKey != null)
            {
                AccessKeyNextCard.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKeyU"];
            if (accessKey != null)
            {
                AccessKeyUpturnedCard.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKeyC"];
            if (accessKey != null)
            {
                AccessKeyClubsPile.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKeyD"];
            if (accessKey != null)
            {
                AccessKeyDiamondsPile.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKeyH"];
            if (accessKey != null)
            {
                AccessKeyHeartsPile.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKeyS"];
            if (accessKey != null)
            {
                AccessKeySpadesPile.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey1"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile1.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey2"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile2.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey3"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile3.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey4"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile4.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey5"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile5.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey6"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile6.Text = accessKey;
            }

            accessKey = (string)localSettings.Values["AccessKey7"];
            if (accessKey != null)
            {
                AccessKeyDealtCardPile7.Text = accessKey;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Barker: Validate the settings. Detect missing values and duplicates.

            localSettings.Values["AccessKeyN"] = AccessKeyNextCard.Text.ToUpper();
            localSettings.Values["AccessKeyU"] = AccessKeyUpturnedCard.Text.ToUpper();
            localSettings.Values["AccessKeyC"] = AccessKeyClubsPile.Text.ToUpper();
            localSettings.Values["AccessKeyD"] = AccessKeyDiamondsPile.Text.ToUpper();
            localSettings.Values["AccessKeyH"] = AccessKeyHeartsPile.Text.ToUpper();
            localSettings.Values["AccessKeyS"] = AccessKeySpadesPile.Text.ToUpper();
            localSettings.Values["AccessKey1"] = AccessKeyDealtCardPile1.Text.ToUpper();
            localSettings.Values["AccessKey2"] = AccessKeyDealtCardPile2.Text.ToUpper();
            localSettings.Values["AccessKey3"] = AccessKeyDealtCardPile3.Text.ToUpper();
            localSettings.Values["AccessKey4"] = AccessKeyDealtCardPile4.Text.ToUpper();
            localSettings.Values["AccessKey5"] = AccessKeyDealtCardPile5.Text.ToUpper();
            localSettings.Values["AccessKey6"] = AccessKeyDealtCardPile6.Text.ToUpper();
            localSettings.Values["AccessKey7"] = AccessKeyDealtCardPile7.Text.ToUpper();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            AccessKeyNextCard.Text = "N";
            AccessKeyUpturnedCard.Text = "U";

            AccessKeyClubsPile.Text = "C";
            AccessKeyDiamondsPile.Text = "D";
            AccessKeyHeartsPile.Text = "H";
            AccessKeySpadesPile.Text = "S";

            AccessKeyDealtCardPile1.Text = "1";
            AccessKeyDealtCardPile2.Text = "2";
            AccessKeyDealtCardPile3.Text = "3";
            AccessKeyDealtCardPile4.Text = "4";
            AccessKeyDealtCardPile5.Text = "5";
            AccessKeyDealtCardPile6.Text = "6";
            AccessKeyDealtCardPile7.Text = "7";
        }
    }
}
