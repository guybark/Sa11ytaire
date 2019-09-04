// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

using Windows.ApplicationModel.Resources;

using Microsoft.Xbox.Services.System;
using Microsoft.Xbox.Services;

using Sol4All.AzureCognitiveServices;
using Sol4All.Classes;
using Sol4All.ViewModels;

// Barker: What about minimimum contrast in light and dark themes?
// Barker: Make keyboard focus feedback more prominent?

namespace Sol4All
{
    public enum CardState
    {
        KingPlaceHolder,
        FaceDown,
        FaceUp
    }

    public enum Suit
    {
        NoSuit,
        Clubs,
        Diamonds,
        Hearts,
        Spades,
    }

    public sealed partial class MainPage : Page
    {
        public ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private int cCardPiles = 7;

        private Shuffler _shuffler;
        private List<Card> _deckRemaining = new List<Card>();
        private List<Card> _deckUpturned = new List<Card>();

        private int cTargetPiles = 4;
        private List<Card>[] _targetPiles = new List<Card>[4];

        private AccessibilitySettings accessibilitySettings;

        public PlayingCardViewModel ViewModel { get; set; }

        public Notifications notifications;
        private string NotificationActivityID_Default = "060C85B1-3207-4AC3-942B-AC3FE47564C7";

        private bool cardHasBeenMoved = false;

        // If a gamepad shoulder button is pressed, have a press of a number key 
        // be equivalent to a press of a function key.
        private bool _numberKeysSimulateFunctionKeys = false;

        private string[] localizedNumbers = new string[10];

        private bool firstMoveToDealtCardPile = true;

        public MainPage()
        {
            this.InitializeComponent();

            var resourceLoader = new ResourceLoader();
            for (int i = 0; i < 10; i++)
            {
                localizedNumbers[i] = resourceLoader.GetString((i + 1).ToString());
            }

            // All Xbox Creators apps are required to sign-in, regardless of whether 
            // they're running on an Xbox or PC.
            //SignIn();

            this.ViewModel = new PlayingCardViewModel();
            PlayingCards1.Source = this.ViewModel.PlayingCards1;
            PlayingCards2.Source = this.ViewModel.PlayingCards2;
            PlayingCards3.Source = this.ViewModel.PlayingCards3;
            PlayingCards4.Source = this.ViewModel.PlayingCards4;
            PlayingCards5.Source = this.ViewModel.PlayingCards5;
            PlayingCards6.Source = this.ViewModel.PlayingCards6;
            PlayingCards7.Source = this.ViewModel.PlayingCards7;

            accessibilitySettings = new AccessibilitySettings();
            accessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;

            ApplicationView.PreferredLaunchViewSize = new Size(1550, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            SetCardPileSize();

            this.SizeChanged += MainPage_SizeChanged;

            for (int i = 0; i < cTargetPiles; ++i)
            {
                _targetPiles[i] = new List<Card>();
            }

            RestartGame(false /* screenReaderAnnouncement. */);

            this.KeyDown += MainPage_KeyDown;
            this.CharacterReceived += MainPage_CharacterReceived;

            for (double speed = 0.5; speed <= 2.0; speed += 0.25)
            {
                SwitchScanSpeedComboBox.Items.Add(speed);
            }

            SwitchScanSpeedComboBox.SelectedIndex = 0;
            SwitchScanSpeedComboBox.SelectionChanged += SwitchScanSpeedComboBox_SelectionChanged;

            LoadSettings();

            // Barker: Try to avoid all this repetition. The "sender" and "e" passed into
            // the event handlers are both null.
            CardPile1.LayoutUpdated += CardPile1_LayoutUpdated;
            CardPile2.LayoutUpdated += CardPile2_LayoutUpdated;
            CardPile3.LayoutUpdated += CardPile3_LayoutUpdated;
            CardPile4.LayoutUpdated += CardPile4_LayoutUpdated;
            CardPile5.LayoutUpdated += CardPile5_LayoutUpdated;
            CardPile6.LayoutUpdated += CardPile6_LayoutUpdated;
            CardPile7.LayoutUpdated += CardPile7_LayoutUpdated;

            notifications = new Notifications();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            LoadAccessKeys();
        }

        static private XboxLiveUser primaryUser;
        static private XboxLiveContext xboxLiveContext;

        private bool _signingIn;

        private void PlayerSignIn_Click(object sender, RoutedEventArgs e)
        {
            if (_signingIn)
            {
                return;
            }

            _signingIn = true;

            SignIn();

            _signingIn = false;
        }

        public async Task SignIn()
        {
            Debug.WriteLine("Device is: " + Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily);

            PlayerSignIn.Visibility = Visibility.Collapsed;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            PlayerTagTextBlock.Text = resourceLoader.GetString("SigningIn");

            PlayerTagTextBlock.Visibility = Visibility.Visible;

            bool signedIn = false;

            // Get a list of the active Windows users.
            IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();

            // Barker: In tests, the count of users was 1.

            // Acquire the CoreDispatcher which will be required for SignInSilentlyAsync and SignInAsync.
            Windows.UI.Core.CoreDispatcher UIDispatcher = Windows.UI.Xaml.Window.Current.CoreWindow.Dispatcher;

            try
            {
                // Barker: In tests, Status here was RemotelyAuthenticated.

                // 1. Create an XboxLiveUser object to represent the user
                primaryUser = new XboxLiveUser(users[0]);

                Debug.WriteLine("Calling SignInSilentlyAsync...");

                // 2. Sign-in silently to Xbox Live
                SignInResult signInSilentResult = await primaryUser.SignInSilentlyAsync(UIDispatcher);

                Debug.WriteLine("SignInSilentlyAsync Status is: " + signInSilentResult.Status);

                // Barker: In tests, signInSilentResult.Status was UserInteractionRequired.

                switch (signInSilentResult.Status)
                {
                    case SignInStatus.Success:

                        signedIn = true;
                        break;

                    case SignInStatus.UserInteractionRequired:

                        Debug.WriteLine("Calling SignInAsync...");

                        //3. Attempt to sign-in with UX if required
                        SignInResult signInLoud = await primaryUser.SignInAsync(UIDispatcher);

                        Debug.WriteLine("SignInAsync Status is: " + signInLoud.Status);

                        switch (signInLoud.Status)
                        {
                            case SignInStatus.Success:

                                signedIn = true;
                                break;

                            case SignInStatus.UserCancel:

                                // present in-game UX that allows the user to retry the sign-in operation. 
                                // (For example, a sign-in button)

                                PlayerSignIn.Visibility = Visibility.Visible;
                                break;

                            default:
                                break;
                        }

                        break;

                    default:
                        break;
                }

                if (signedIn)
                {
                    Debug.WriteLine("Creating XboxLiveContext...");

                    // 4. Create an Xbox Live context based on the interacting user

                    // NO! With this line we get a NullReferenceException periodically 
                    // in Microsoft.Xbox.Services.dll. So remove it. In fact, remove the 
                    // SignOutCompleted too, given that the user is signed in for the 
                    // duration of the game.

                    //xboxLiveContext = new XboxLiveContext(primaryUser);

                    Debug.WriteLine("Created XboxLiveContext");

                    Debug.WriteLine("Gamer tag is: " + primaryUser.Gamertag);

                    PlayerTagTextBlock.Text = primaryUser.Gamertag;

                    //add the sign out event handler
                    // XboxLiveUser.SignOutCompleted += OnSignOut;
                }
                else
                {
                    PlayerTagTextBlock.Text = "";
                    PlayerTagTextBlock.Visibility = Visibility.Collapsed;

                    PlayerSignIn.Visibility = Visibility.Visible;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        //public void OnSignOut(object sender, SignOutCompletedEventArgs e)
        //{
        //    Debug.WriteLine("In OnSignOut");

        //    // 6. When the game exits or the user signs-out, release the XboxLiveUser object and XboxLiveContext object by setting them to null
        //    primaryUser = null;
        //    xboxLiveContext = null;

        //    Debug.WriteLine("Leaving OnSignOut");
        //}

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.Handled)
            {
                return;
            }

            args.Handled = true;

            switch (args.VirtualKey)
            {
                case VirtualKey.GamepadLeftShoulder:

                    ToggleSingleKeyToMove();
                    break;

                case VirtualKey.GamepadRightShoulder:

                    _numberKeysSimulateFunctionKeys = true;

                    if (this.ViewModel.ScanModeOn)
                    {
                        if (_dlgSwitchScanTimer != null)
                        {
                            // A dialog is up, so invoke the focussed button.
                            var buttonWithFocus = FocusManager.GetFocusedElement() as Button;
                            if (buttonWithFocus != null)
                            {
                                ButtonAutomationPeer peer = FrameworkElementAutomationPeer.FromElement(buttonWithFocus) as ButtonAutomationPeer;
                                if (peer != null)
                                {
                                    peer.Invoke();
                                }
                            }
                        }
                        else
                        {
                            HandleSwitchPress();
                        }
                    }
                    else
                    {
                        HandleF6(args.VirtualKey == VirtualKey.GamepadRightShoulder);
                    }

                    break;

                case VirtualKey.GamepadY:

                    StopScan();

                    this.ViewModel.ScanModeOn = !this.ViewModel.ScanModeOn;

                    var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();

                    RaiseNotificationEvent(
                        AutomationNotificationKind.Other,
                        AutomationNotificationProcessing.ImportantAll,
                        resourceLoader.GetString("SwitchControl") + " " +
                            (this.ViewModel.ScanModeOn ?
                                resourceLoader.GetString("On") :
                                resourceLoader.GetString("Off")),
                        NotificationActivityID_Default,
                        NextCardDeck);

                    localSettings.Values["ScanMode"] = this.ViewModel.ScanModeOn;

                    break;

                case VirtualKey.GamepadX:

                    StopScan();

                    for (int i = 0; i < cCardPiles; i++)
                    {
                        ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                        list.SelectedItem = null;
                    }

                    UncheckToggleButtons(true);

                    break;

                default:
                    args.Handled = false;
                    break;
            }
        }

        private void SimulateTab(bool forward)
        {
            if (forward)
            {
                var itemWithFocus = FocusManager.GetFocusedElement() as ListViewItem;
                if (itemWithFocus != null)
                {
                    PlayingCard card = itemWithFocus.Content as PlayingCard;
                    if (card.ListIndex == 7)
                    {
                        NextCardDeck.Focus(FocusState.Keyboard);

                        return;
                    }
                }
            }

            Control targetControl = FocusManager.FindNextFocusableElement(forward ?
                                    FocusNavigationDirection.Next : FocusNavigationDirection.Previous) as Control;
            if (targetControl != null)
            {
                targetControl.Focus(FocusState.Keyboard);
            }
        }

        private void HandleF6(bool forward)
        {
            Control focusedControl = (Control)FocusManager.GetFocusedElement();
            if (focusedControl != null)
            {
                if (focusedControl.Name.Contains("NextCardDeck"))
                {
                    if (forward)
                    {
                        TargetPileC.Focus(FocusState.Keyboard);
                    }
                    else
                    {
                        if (ShowNotificationsPanel.Visibility == Visibility.Visible)
                        {
                            MostRecentNotificationTextBox.Focus(FocusState.Keyboard);
                        }
                        else
                        {
                            CardPile1.Focus(FocusState.Keyboard);
                        }
                    }
                }
                else if (focusedControl.Name.Contains("TargetPile"))
                {
                    if (forward)
                    {
                        CardPile1.Focus(FocusState.Keyboard);
                    }
                    else
                    {
                        NextCardDeck.Focus(FocusState.Keyboard);
                    }
                }
                else if (focusedControl.Name.Contains("MostRecentNotificationTextBox"))
                {
                    if (forward)
                    {
                        NextCardDeck.Focus(FocusState.Keyboard);
                    }
                    else
                    {
                        CardPile1.Focus(FocusState.Keyboard);
                    }
                }
                else // Focus is on dealt card piles.
                {
                    if (forward)
                    {
                        if (ShowNotificationsPanel.Visibility == Visibility.Visible)
                        {
                            MostRecentNotificationTextBox.Focus(FocusState.Keyboard);
                        }
                        else
                        {
                            NextCardDeck.Focus(FocusState.Keyboard);
                        }
                    }
                    else
                    {
                        TargetPileC.Focus(FocusState.Keyboard);
                    }
                }
            }
        }

        public void LoadSettings()
        {
            try
            {
                this.ViewModel.ScanModeOn = (bool)localSettings.Values["ScanMode"];
            }
            catch (Exception)
            {
            }

            try
            {
                SwitchScanSpeedComboBox.SelectedValue = (double)localSettings.Values["SwitchScanSpeed"];
            }
            catch (Exception)
            {
                // Default to the switch control highlight moving once per second.
                SwitchScanSpeedComboBox.SelectedValue = 1.5;
            }

            try
            {
                PlaySoundEffectsCheckBox.IsChecked = (bool)localSettings.Values["PlaySoundEffects"];
            }
            catch (Exception)
            {
            }

            try
            {
                PlayTTSNotificationsCheckBox.IsChecked = (bool)localSettings.Values["PlayTTSNotifications"];
            }
            catch (Exception)
            {
            }

            try
            {
                ShowNotificationWindowCheckBox.IsChecked = (bool)localSettings.Values["ShowNotificationWindow"];
            }
            catch (Exception)
            {
            }

            ShowNotificationsPanel.Visibility = (ShowNotificationWindowCheckBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed);

            try
            {
                EnableAutomaticHintsCheckBox.IsChecked = (bool)localSettings.Values["EnableAutomaticHints"];
            }
            catch (Exception)
            {
            }

            try
            {
                this.ViewModel.SingleKeyToMove = (bool)localSettings.Values["SingleKeyToMove"];
            }
            catch (Exception)
            {
            }

            try
            {
                this.ViewModel.SelectWithoutAltKey = (bool)localSettings.Values["SelectWithoutAltKey"];
            }
            catch (Exception)
            {
            }
        }

        private void SwitchScanSpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            localSettings.Values["SwitchScanSpeed"] = (double)SwitchScanSpeedComboBox.SelectedValue;
        }

        private void CardPile1_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile1);
        }

        private void CardPile2_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile2);
        }

        private void CardPile3_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile3);
        }

        private void CardPile4_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile4);
        }

        private void CardPile5_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile5);
        }

        private void CardPile6_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile6);
        }

        private void CardPile7_LayoutUpdated(object sender, object e)
        {
            SetHeightOfCards(CardPile7);
        }

        private void SetHeightOfCards(ListView list)
        {
            if (list != null)
            {
                for (int i = 0; i < list.Items.Count; ++i)
                {
                    var card = list.Items[i] as PlayingCard;

                    var item = (list.ContainerFromItem(card) as ListViewItem);

                    if (item != null) // Barker: Determine when this can be null.
                    {
                        // Is this the last item in the list?
                        if (i == list.Items.Count - 1)
                        {
                            item.Height = (((item.ActualWidth - item.Padding.Left - item.Padding.Right) * 346) / 259);

                            // Give the last item in this list the same access key as the list itself.
                            string listAccessKey = list.AccessKey;
                            AutomationProperties.SetAccessKey(item, "Alt, " + listAccessKey);
                        }
                        else
                        {
                            // This list item has no access key.
                            AutomationProperties.SetAccessKey(item, "");

                            if (card.CardState == CardState.FaceDown)
                            {
                                item.MinHeight = 20;
                                item.Height = 20;
                            }
                            else
                            {
                                item.Height = (item.ActualWidth * 346) / (259 * 4);
                            }
                        }
                    }
                }
            }
        }

        private void AccessibilitySettings_HighContrastChanged(AccessibilitySettings sender, object args)
        {
            // Barker: Refresh the app's UI here.            
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetCardPileSize();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetCardPileSize();
        }

        private void SetCardPileSize()
        {
            if (this.ActualWidth <= 0)
            {
                return;
            }

            // On Xbox, this.ActualWidth seems too wide.

            double width;

            bool chatVisible = (QnAWebView.Visibility == Visibility.Visible) || (QnAImage.Visibility == Visibility.Visible);

            double AvailableWidth = this.ActualWidth - (chatVisible ? 500 : 0);

            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
            {
                width = (AvailableWidth - 20) / 7;
            }
            else
            {
                double adjustedWidth = (AvailableWidth * 7) / 8;
                width = (adjustedWidth - 20) / 7;
            }

            NextCardDeck.Width = width;

            UpturnedCardsGrid.Width = width;
            CardDeckUpturned.Width = width;
            CardDeckUpturnedObscuredLower.Width = width;
            CardDeckUpturnedObscuredHigher.Width = width;

            TargetPileC.Width = width;
            TargetPileD.Width = width;
            TargetPileH.Width = width;
            TargetPileS.Width = width;

            int dealtCardPileWidth = (int)Math.Max(width, 20);
            CardPile1.Width = dealtCardPileWidth;
            CardPile2.Width = dealtCardPileWidth;
            CardPile3.Width = dealtCardPileWidth;
            CardPile4.Width = dealtCardPileWidth;
            CardPile5.Width = dealtCardPileWidth;
            CardPile6.Width = dealtCardPileWidth;
            CardPile7.Width = dealtCardPileWidth;

            // Prevent the app from flashing due to a resize of the TopCornerPiles.
            TopCornerPiles.Height = (this.ActualHeight / 2) - 20;

            // Barker: Figure out how to get the Height to work through binding.
            double obscuredCardsHeight = this.ActualHeight / 12;
            CardDeckUpturnedObscuredLower.Height = obscuredCardsHeight;
            CardDeckUpturnedObscuredHigher.Height = obscuredCardsHeight;
        }

        private void ClearTargetPileButtons()
        {
            TargetPileC.Card = null;
            TargetPileD.Card = null;
            TargetPileH.Card = null;
            TargetPileS.Card = null;
        }

        private bool _dealingCards = false;

        private void DealCards()
        {
            _dealingCards = true;

            int cardIndex = 0;

            Debug.WriteLine("Deal, start with " + _deckRemaining.Count + " cards.");

            for (int i = 0; i < cCardPiles; i++)
            {
                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                if (list.Items.Count > 0)
                {
                    list.SelectedIndex = 0;
                }

                this.ViewModel.PlayingCards[i].Clear();

                for (int j = 0; j < (i + 1); j++)
                {
                    var card = new PlayingCard();

                    card.Card = _deckRemaining[cardIndex];

                    EnableCard(card, (j == i));

                    card.InitialIndex = j + 1;

                    card.ListIndex = i + 1;

                    ++cardIndex;

                    this.ViewModel.PlayingCards[i].Add(card);
                }
            }

            _deckRemaining.RemoveRange(0, cardIndex);

            Debug.WriteLine("Left with " + _deckRemaining.Count + " cards remaining.");

            for (int i = 0; i < cTargetPiles; ++i)
            {
                _targetPiles[i].Clear();
            }

            ClearTargetPileButtons();
            ClearUpturnedPileButton();

            _dealingCards = false;
        }

        private void ClearUpturnedPileButton()
        {
            SetUpturnedCardsVisuals();
        }

        private void UncheckToggleButtons(bool includeUpturnedCard)
        {
            if (includeUpturnedCard)
            {
                CardDeckUpturned.IsChecked = false;
            }

            TargetPileC.IsChecked = false;
            TargetPileD.IsChecked = false;
            TargetPileH.IsChecked = false;
            TargetPileS.IsChecked = false;
        }

        private void RestartGame(bool screenReaderAnnouncement)
        {
            MostRecentNotificationTextBox.Text = "";

            UncheckToggleButtons(true);

            _deckUpturned.Clear();

            CardDeckUpturned.Card = null;
            CardDeckUpturnedObscuredLower.Card = null;
            CardDeckUpturnedObscuredHigher.Card = null;

            SetUpturnedCardsVisuals();

            _deckRemaining.Clear();

            for (int rank = 1; rank <= 13; ++rank)
            {
                foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                {
                    if (suit == Suit.NoSuit)
                    {
                        continue;
                    }

                    _deckRemaining.Add(new Card { Rank = rank, Suit = suit });
                }
            }

            _shuffler = new Shuffler();
            _shuffler.Shuffle(_deckRemaining);

            DealCards();

            NextCardDeck.IsEmpty = false;

            NextCardDeck.Focus(FocusState.Keyboard);

            if (screenReaderAnnouncement)
            {
                RaiseNotificationEvent(
                    AutomationNotificationKind.Other,
                    AutomationNotificationProcessing.ImportantMostRecent,
                    "Game restarted",
                    NotificationActivityID_Default,
                    NextCardDeck);
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            RestartGame(true /* screenReaderAnnouncement */);
        }

        private async void ScanModeRestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartSwitchScanDlgTimer();

            ContentDialogResult result = await queryDialog.ShowAsync();

            StopSwitchScanDlgTimer();

            if (result == ContentDialogResult.Primary)
            {
                RestartGame(true /* screenReaderAnnouncement */);
            }
        }

        private void SwitchModeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GameAppBar.IsOpen = true;
        }

        private void dlgScanTimer_Tick(object sender, object e)
        {
            // Explicitly call Focus() here, rather than something like TryMoveFocus(),
            // because it seems that keyboard focus visuals can stop appearing unless we
            // have the opportunity to specify FocusState.Keyboard.

            Button nextButton = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next) as Button;
            if (nextButton != null)
            {
                nextButton.Focus(FocusState.Keyboard);
            }
        }

        private int GetTargetPileListIndex(CardPileToggleButton tergetPileButton)
        {
            int index = -1;

            string pileId = tergetPileButton.Name.Replace("TargetPile", "");
            switch (pileId)
            {
                case "C":
                    index = 0;
                    break;
                case "D":
                    index = 1;
                    break;
                case "H":
                    index = 2;
                    break;
                case "S":
                    index = 3;
                    break;
            }

            return index;
        }

        private void SetUpturnedCardsVisuals()
        {
            if (_deckUpturned.Count == 0)
            {
                CardDeckUpturned.IsChecked = false;
                CardDeckUpturned.IsEnabled = false;

                CardDeckUpturned.Card = null;
                CardDeckUpturnedObscuredHigher.Card = null;
                CardDeckUpturnedObscuredLower.Card = null;
            }
            else
            {
                CardDeckUpturned.IsEnabled = true;

                CardDeckUpturned.Card = _deckUpturned[_deckUpturned.Count - 1];

                if (_deckUpturned.Count > 1)
                {
                    CardDeckUpturnedObscuredHigher.Card = _deckUpturned[_deckUpturned.Count - 2];
                }
                else
                {
                    CardDeckUpturnedObscuredHigher.Card = null;
                }

                if (_deckUpturned.Count > 2)
                {
                    CardDeckUpturnedObscuredLower.Card = _deckUpturned[_deckUpturned.Count - 3];
                }
                else
                {
                    CardDeckUpturnedObscuredLower.Card = null;
                }
            }
        }

        // Barker: Use the approved method of getting the items source.
        private ObservableCollection<PlayingCard> GetListSource(ListView list)
        {
            ObservableCollection<PlayingCard> col = null;

            int index = int.Parse(list.Name.Replace("CardPile", ""));
            col = ViewModel.PlayingCards[index - 1];

            return col;
        }

        private void EnableCard(PlayingCard card, bool enable)
        {
            card.FaceDown = !enable;
            card.CardState = (enable ? CardState.FaceUp : CardState.FaceDown);
        }

        private void MainPage_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
        {
            e.Handled = true;

            if (_numberKeysSimulateFunctionKeys)
            {
                HandleChatdownKeyDown(e);
            }
            else if (this.ViewModel.SelectWithoutAltKey)
            {
                // Barker: Rename HandleChatdownKeyDown() now that it's used on the PC.
                HandleChatdownKeyDown(e);
            }
            else
            {
                e.Handled = false;
            }
        }

        private void DeselectDealtCards()
        {
            for (int i = 0; i < cCardPiles; i++)
            {
                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                list.SelectedItem = null;
            }
        }

        VisionService visionService = new VisionService();

        private void ToggleStateOfImageProcessing()
        {
            SpeechInputStatus.Text = "";

            visionService.ToggleStateOfImageProcessing(this);
        }

        public void SetLookingStatus(string status)
        {
            SpeechInputStatus.Text = status;
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var ctrlState = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            bool ctrlDown = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
            if (ctrlDown)
            {
                // Ctrl+S toggles the state of Azure Speech reco.
                if (e.Key == VirtualKey.S)
                {
                    AzureToggleListeningForSpeech();

                    return;
                }
                else if (e.Key == VirtualKey.V)
                {
                    // Toggle the state of image processing.
                    ToggleStateOfImageProcessing();
                }
                else if (e.Key == VirtualKey.C)
                {
                    // Toggle the state of chatting with Sa11y.
                    ShowBotCheckBox.IsChecked = ((bool)ShowBotCheckBox.IsChecked ? false : true);
                }
                else if (e.Key == VirtualKey.W)
                {
                    // Toggle which UI gets shown when interacting with the Sa11y bot.
                    chatDirect = !chatDirect;
                }
            }

            var shiftState = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            bool shiftDown = shiftState.HasFlag(CoreVirtualKeyStates.Down);

            if (e.Key == VirtualKey.Escape)
            {
                SpeechInputStatus.Text = "";
                MostRecentNotificationTextBox.Text = "";

                ReactToCancel();
            }
            else if (e.Key == VirtualKey.GamepadLeftThumbstickButton)
            {
                Control controlWithFocus = FocusManager.GetFocusedElement() as Control;
                if (controlWithFocus != null)
                {
                    ListViewItem item = controlWithFocus as ListViewItem;
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                    else
                    {
                        ToggleButton toggleButton = controlWithFocus as ToggleButton;
                        if (toggleButton != null)
                        {
                            toggleButton.IsChecked = true;
                        }
                        else
                        {
                            Button button = controlWithFocus as Button;
                            if (button != null)
                            {
                                DoNextCardClick();
                            }
                        }
                    }
                }
            }
            else if (e.Key == VirtualKey.F1)
            {
                LaunchHelp();
            }
            else if ((e.Key == VirtualKey.F2) || (e.Key == VirtualKey.GamepadRightThumbstickLeft))
            {
                AnnounceStateRemainingCards();
            }
            else if ((e.Key == VirtualKey.F3) || (e.Key == VirtualKey.GamepadRightThumbstickUp))
            {
                AnnounceStateTargetPiles();
            }
            else if ((e.Key == VirtualKey.F4) || (e.Key == VirtualKey.GamepadRightThumbstickRight))
            {
                AnnounceStateDealtCardPiles();
            }
            else if ((e.Key == VirtualKey.F5) || (e.Key == VirtualKey.GamepadRightThumbstickButton))
            {
                RestartGame(true /* screenReaderAnnouncement */);
            }
            else if ((e.Key == VirtualKey.F6))
            {
                HandleF6(!shiftDown);
            }
            else if ((e.Key == VirtualKey.F7) || (e.Key == VirtualKey.GamepadRightThumbstickDown))
            {
                CanAMoveBeMade();
            }
            else if (e.Key == VirtualKey.F8) // This is toggled on Xbox with a left bumper press.
            {
                ToggleSingleKeyToMove();
            }
            else if (e.Key == VirtualKey.F9) // This is activated on Xbox with a right bumper press.
            {
                ToggleRequireAlt();
            }
            else if (e.Key == VirtualKey.F10)
            {
                ToggleSwitchScanMode();
            }
            else if (e.Key == VirtualKey.Right)
            {
                SimulateTab(true);
            }
            else if (e.Key == VirtualKey.Left)
            {
                SimulateTab(false);
            }
            else
            {
                e.Handled = false;
            }
        }
        
        private void ReactToCancel()
        {
            StopScan();

            DeselectDealtCards();

            UncheckToggleButtons(true);
        }

        private void ToggleSwitchScanMode()
        {
            StopScan();

            this.ViewModel.ScanModeOn = !this.ViewModel.ScanModeOn;

            RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.ImportantAll,
                "Switch control is now " + (this.ViewModel.ScanModeOn ? "on" : "off"),
                NotificationActivityID_Default,
                NextCardDeck);

            localSettings.Values["ScanMode"] = this.ViewModel.ScanModeOn;

            // Resize areas based on the visibility of the scan-related UI area.
            SetCardPileSize();
        }

        private void ToggleSingleKeyToMove()
        {
            this.ViewModel.SingleKeyToMove = !this.ViewModel.SingleKeyToMove;

            localSettings.Values["SingleKeyToMove"] = this.ViewModel.SingleKeyToMove;
        }

        private void ToggleRequireAlt()
        {
            this.ViewModel.SelectWithoutAltKey = !this.ViewModel.SelectWithoutAltKey;

            localSettings.Values["SelectWithoutAltKey"] = this.ViewModel.SelectWithoutAltKey;
        }

        private void HandleChatdownKeyDown(CharacterReceivedRoutedEventArgs e)
        {
            e.Handled = true;

            string mappedKey = "";

            string upperCaseKey = e.Character.ToString().ToUpper();

            try
            {
                mappedKey = currentAccessKeys.FirstOrDefault(x => x.Value == upperCaseKey).Key;
            }
            catch (Exception ex)
            {

            }

            switch (mappedKey)
            {
                case "N":

                    DoNextCardClick();
                    break;

                case "U":

                    CardDeckUpturned.IsChecked = true;
                    break;

                case "C":

                    TargetPileC.IsChecked = true;
                    break;

                case "D":

                    TargetPileD.IsChecked = true;
                    break;

                case "H":

                    TargetPileD.IsChecked = true;
                    break;

                case "S":

                    TargetPileS.IsChecked = true;
                    break;

                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":

                    int index = (int)mappedKey[0] - (int)'1';

                    ListView list = (ListView)CardPileGrid.FindName("CardPile" + (index + 1));
                    int count = list.Items.Count;

                    if (count > 0)
                    {
                        list.SelectedIndex = count - 1;

                        list.Focus(FocusState.Keyboard);
                    }

                    break;

                default:

                    e.Handled = false;
                    break;
            }

            return;
        }

        private async void LaunchHelp()
        {
            string helpFile = @"Assets\HelpContent.htm";

            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            StorageFile file = await InstallationFolder.GetFileAsync(helpFile);
            if (file != null)
            {
                var options = new Windows.System.LauncherOptions();
                options.DisplayApplicationPicker = false;

                var success = await Windows.System.Launcher.LaunchFileAsync(file, options);

                Debug.WriteLine("Help file launch result: " + success);
            }
        }

        private void AnnounceStateRemainingCards()
        {
            string stateMessage = "";

            var resourceLoader = new ResourceLoader();

            if (_deckUpturned.Count > 0)
            {
                stateMessage += resourceLoader.GetString("TopUpturnedCardIs") + " " + 
                    _deckUpturned[_deckUpturned.Count - 1].ToString();
            }

            if (_deckUpturned.Count > 1)
            {
                stateMessage += ", " + resourceLoader.GetString("Then") + " " + 
                    _deckUpturned[_deckUpturned.Count - 2].ToString();
            }

            if (_deckUpturned.Count > 2)
            {
                stateMessage += ", " + resourceLoader.GetString("Then") + " " +
                    _deckUpturned[_deckUpturned.Count - 3].ToString();
            }

            if (_deckUpturned.Count > 0)
            {
                stateMessage += ". ";
            }

            if (_deckUpturned.Count == 0)
            {
                stateMessage = resourceLoader.GetString("ThereAreNoUpturnedCards") + ".";
            }

            if (_deckRemaining.Count > 0)
            {
                stateMessage += " " + resourceLoader.GetString("MoreCardsAreAvailable") + ".";
            }

            RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.All,
                stateMessage,
                NotificationActivityID_Default,
                NextCardDeck);
        }

        private void AnnounceStateTargetPiles()
        {
            var resourceLoader = new ResourceLoader();

            string stateMessage = resourceLoader.GetString("TargetPiles") + ", ";

            string empty = resourceLoader.GetString("Empty");
            string pile = resourceLoader.GetString("Pile");

            stateMessage = 
                (TargetPileC.Card == null ? 
                    empty + " " + resourceLoader.GetString("Clubs") + " " + pile : TargetPileC.Card.ToString()) + ", " +
                (TargetPileD.Card == null ?
                    empty + " " + resourceLoader.GetString("Diamonds") + " " + pile : TargetPileD.Card.ToString()) + ", " +
                (TargetPileH.Card == null ?
                    empty + " " + resourceLoader.GetString("Hearts") + " " + pile : TargetPileH.Card.ToString()) + ", " +
                (TargetPileS.Card == null ?
                    empty + " " + resourceLoader.GetString("Spades") + " " + pile : TargetPileS.Card.ToString()) + ".";

            RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.All,
                stateMessage,
                NotificationActivityID_Default,
                NextCardDeck);
        }

        private void AnnounceStateDealtCardPiles()
        {
            string stateMessage = "";

            var resourceLoader = new ResourceLoader();

            string empty = resourceLoader.GetString("Empty");
            string pile = resourceLoader.GetString("Pile");
            string to = resourceLoader.GetString("To");
            string card = resourceLoader.GetString("Card");
            string cards = resourceLoader.GetString("Cards");
            string facedown = resourceLoader.GetString("FaceDown");

            for (int i = 0; i < cCardPiles; i++)
            {
                stateMessage += pile + " " + (i + 1) + ", ";

                int cFaceDown = 0;
                int indexLastFaceUp = -1;

                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                for (int j = list.Items.Count - 1; j >= 0; j--)
                {
                    if (j == list.Items.Count - 1)
                    {
                        if ((list.Items[j] as PlayingCard).CardState == CardState.KingPlaceHolder)
                        {
                            stateMessage += empty;
                        }
                        else
                        {
                            stateMessage += (list.Items[j] as PlayingCard).Card;
                        }
                    }
                    else
                    {
                        if ((list.Items[j] as PlayingCard).FaceDown)
                        {
                            ++cFaceDown;
                        }
                        else
                        {
                            indexLastFaceUp = j;
                        }
                    }
                }

                if ((indexLastFaceUp != -1) && (indexLastFaceUp != list.Items.Count - 1))
                {
                    stateMessage += " " + to + " " + (list.Items[indexLastFaceUp] as PlayingCard).Card;
                }

                stateMessage += ", ";

                if (cFaceDown > 0)
                {
                    stateMessage += cFaceDown + 
                        (cFaceDown > 1 ? cards : card) + " " + facedown + " , ";
                }
            }

            RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.All,
                stateMessage,
                NotificationActivityID_Default,
                NextCardDeck);
        }

        protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
        {
            if ((e.Key == VirtualKey.Space) && this.ViewModel.ScanModeOn)
            {
                HandleSwitchPress();

                e.Handled = true;
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        private bool CanMoveCard(PlayingCard cardBelow, PlayingCard cardAbove)
        {
            bool canMove = false;

            if ((cardBelow != null) && (cardAbove != null))
            {
                if (cardBelow.Card.Rank == cardAbove.Card.Rank + 1)
                {
                    bool isBelowRed = ((cardBelow.Card.Suit == Suit.Diamonds) || (cardBelow.Card.Suit == Suit.Hearts));
                    bool isAboveRed = ((cardAbove.Card.Suit == Suit.Diamonds) || (cardAbove.Card.Suit == Suit.Hearts));

                    canMove = (isBelowRed != isAboveRed);
                }
            }

            return canMove;
        }

        private void CardDeckUpturned_Checked(object sender, RoutedEventArgs e)
        {
            if (_deckUpturned.Count > 0)
            {
                // Always deselect all dealt cards and the target card piles 
                // when the upturned card is selected.
                DeselectDealtCards();
                UncheckToggleButtons(false);

                var resourceLoader = new ResourceLoader();

                string upturnedAnnouncement =
                    resourceLoader.GetString("Upturned") + " " +
                    CardDeckUpturned.Card.ToString() + " " +
                    resourceLoader.GetString("Selected") + ".";

                RaiseNotificationEvent(
                    AutomationNotificationKind.ActionCompleted,
                     AutomationNotificationProcessing.ImportantAll,
                     upturnedAnnouncement,
                     NotificationActivityID_Default,
                     NextCardDeck);

                if (this.ViewModel.SingleKeyToMove)
                {
                    string suggestion;
                    MoveUpturnedCardWithSingleKeyPressIfPossible(false, out suggestion);
                }
            }
        }

        private bool MoveUpturnedCardWithSingleKeyPressIfPossible(bool returnSuggestion, out string suggestion)
        {
            suggestion = "";

            bool movedCard = false;

            if (_deckUpturned.Count == 0)
            {
                return false;
            }

            Card upturnedCard = _deckUpturned[_deckUpturned.Count - 1];

            bool moveCardToTargetPile = false;

            CardPileToggleButton targetCardButton = null;

            if (upturnedCard.Suit == Suit.Clubs)
            {
                targetCardButton = TargetPileC;
            }
            else if (upturnedCard.Suit == Suit.Diamonds)
            {
                targetCardButton = TargetPileD;
            }
            else if (upturnedCard.Suit == Suit.Hearts)
            {
                targetCardButton = TargetPileH;
            }
            else if (upturnedCard.Suit == Suit.Spades)
            {
                targetCardButton = TargetPileS;
            }

            if (targetCardButton != null)
            {
                if (targetCardButton.Card == null)
                {
                    moveCardToTargetPile = (upturnedCard.Rank == 1);
                }
                else
                {
                    moveCardToTargetPile = (upturnedCard.Rank == targetCardButton.Card.Rank + 1);
                }
            }

            if (moveCardToTargetPile)
            {
                movedCard = MoveUpturnedCardToTargetPileAsAppropriate(returnSuggestion, out suggestion, targetCardButton);
            }
            else
            {
                bool moveCardToDealtCardPile = false;

                for (int i = 0; i < cCardPiles; i++)
                {
                    ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                    if (list.Items.Count > 0)
                    {
                        PlayingCard topCardInDealtCardPile = (list.Items[list.Items.Count - 1] as PlayingCard);

                        if (topCardInDealtCardPile.CardState == CardState.KingPlaceHolder)
                        {
                            // Move a King to the empty pile.
                            moveCardToDealtCardPile = (upturnedCard.Rank == 13);
                        }
                        else
                        {
                            PlayingCard playingCardUpturned = new PlayingCard();
                            playingCardUpturned.Card = _deckUpturned[_deckUpturned.Count - 1];

                            if (CanMoveCard(topCardInDealtCardPile, playingCardUpturned))
                            {
                                moveCardToDealtCardPile = true;
                            }
                        }

                        if (moveCardToDealtCardPile)
                        {
                            if (returnSuggestion)
                            {
                                targetCardButton.IsChecked = true;

                                suggestion = upturnedCard.ToString();
                            }
                            else
                            {
                                list.SelectedIndex = list.Items.Count - 1;
                            }
                            
                            movedCard = true;
                        
                            break;
                        }
                    }
                }
            }

            return movedCard;
        }

        public void RaiseNotificationEvent(AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string textString, string activityId, UIElement element)
        {
            var outputDirectToTTS = (PlayTTSNotificationsCheckBox.IsChecked == true);

            notifications.RaiseNotificationEvent(
                notificationKind, 
                notificationProcessing, 
                textString, 
                activityId, 
                element, 
                outputDirectToTTS
                );

            // Always set the most recent notification text even if it's not currently visible.
            // The customer may next show the text to learn what the notification was.
            MostRecentNotificationTextBox.Text = textString;
        }

        private void SingleKeyToMoveCardCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            ToggleSingleKeyToMove();
        }

        private void SelectCardWithoutAltKeyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            ToggleRequireAlt();
        }

        private void EnableAutomaticHintsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            localSettings.Values["EnableAutomaticHints"] = (sender as CheckBox).IsChecked;
        }

        private void PlayTTSNotificationsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            localSettings.Values["PlayTTSNotifications"] = (sender as CheckBox).IsChecked;
        }

        private void PlaySoundEffectsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            localSettings.Values["PlaySoundEffects"] = (sender as CheckBox).IsChecked;
        }

        private void ShowNotificationWindowCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            bool showNotifications = (sender as CheckBox).IsChecked.Value;

            localSettings.Values["ShowNotificationWindow"] = showNotifications;

            ShowNotificationsPanel.Visibility = (showNotifications ? Visibility.Visible : Visibility.Collapsed);

            // Resize areas based on the visibility of the notifications UI area.
            SetCardPileSize();
        }

        private void LaunchHelpButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._scanInProgress)
            {
                GameAppBar.IsOpen = false;
            }

            LaunchHelp();
        }

        private DispatcherTimer _dlgSwitchScanTimer;

        private void StartSwitchScanDlgTimer()
        {
            if (ViewModel.ScanModeOn)
            {
                _dlgSwitchScanTimer = new DispatcherTimer();
                _dlgSwitchScanTimer.Tick += dlgScanTimer_Tick;

                double switchScanSpeed = (double)SwitchScanSpeedComboBox.SelectedValue;
                int seconds = (int)switchScanSpeed;
                int milliseconds = (int)((switchScanSpeed - seconds) * 1000);
                _dlgSwitchScanTimer.Interval = new TimeSpan(0, 0, 0, seconds, milliseconds);

                _dlgSwitchScanTimer.Start();
            }
        }

        private void StopSwitchScanDlgTimer()
        {
            if (_dlgSwitchScanTimer != null)
            {
                _dlgSwitchScanTimer.Stop();

                _dlgSwitchScanTimer = null;
            }
        }

        private async void ShowEndOfGameDialog()
        {
            StartSwitchScanDlgTimer();

            var results = await endOfGameDialog.ShowAsync();

            StopSwitchScanDlgTimer();

            switch (results)
            {
                case ContentDialogResult.Primary:
                    {
                        // user chose to start a new game
                        RestartGame(true /* screenReaderAnnouncement */);
                        break;
                    }
                case ContentDialogResult.Secondary:
                    {
                        //end the game by exiting the application
                        Application.Current.Exit();
                        break;
                    }
            }
        }

        private void SwitchScanSpeedComboBox_DropDownClosed(object sender, object e)
        {
            if (_scanTimer != null)
            {
                double switchScanSpeed = (double)SwitchScanSpeedComboBox.SelectedValue;
                int seconds = (int)switchScanSpeed;
                int milliseconds = (int)((switchScanSpeed - seconds) * 1000);
                _scanTimer.Interval = new TimeSpan(0, 0, 0, seconds, milliseconds);
            }

            if (_scanInProgress)
            {
                GameAppBar.IsOpen = false;

                HandleScanTimerTick();
            }
        }

        private async void SetAccessKeyMappingButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new MapAccessKeysDialog(localSettings);
            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                LoadAccessKeys();
            }
        }

        private void GameAppBar_Opened(object sender, object e)
        {
            if (_dlgSwitchScanTimer == null)
            {
                RestartGameButton.Focus(FocusState.Keyboard);
            }
        }

        // Barker: Is there a more approved way of knowing that a ContentDialog is open?
        private ContentDialog openDlg;

        private void appDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            openDlg = sender;
        }

        private void appDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            openDlg = null;
        }

        // If communication with the Sa11y bot uses a Direct Line channel, then we won't
        // show the bot web chat UI.
        private bool chatDirect = true;

        private void ShowBotCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool botVisible = ((sender as CheckBox).IsChecked == true);

            if (chatDirect)
            {
                if (botVisible)
                {
                    QnAImage.Visibility = Visibility.Visible;

                    // Don't automatically start listening. Instead wait for the command. 
                    // AzureToggleListeningForSpeech();
                }
                else
                {
                    QnAImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Toggle the visibility of the Azure bot web chat UI.
                if (botVisible)
                {
                    QnAWebView.Visibility = Visibility.Visible;

                    QnAWebView.Focus(FocusState.Keyboard);
                }
                else
                {
                    QnAWebView.Visibility = Visibility.Collapsed;
                }
            }

            // If we're turning off the bot, make sure the area used for 
            // showing text is in the appropriate state.
            if (!botVisible)
            {
                ShowNotificationsPanel.Visibility = (ShowNotificationWindowCheckBox.IsChecked.Value ? 
                    Visibility.Visible : Visibility.Collapsed);
            }

            SetCardPileSize();
        }
    }
}