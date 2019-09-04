// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Sol4All.Classes;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Sol4All
{
    public sealed partial class MainPage : Page
    {
        private bool _scanInProgress { get; set; }
        private DispatcherTimer _scanTimer;
        private Panel panelHighlighted;
        private bool _inScanTimer = false;
        private bool _subScanOn = false;
        private int _scanLoopCount = 0;
        private int _subScanLoopCount = 0;
        private int _subScanIndexHighlight = 0;
        private Control _subScanElementHighlighted;
        private bool _scanWithinCardPile;
        private int _scanAtListIndex;
        private int _firstScanAtListIndex;
        private PlayingCard _cardBeingScanned;
        private bool _scanningWithinSwitchControlPanel = false;

        private void StartScan()
        {
            StopScan();

            if (_scanTimer == null)
            {
                _scanTimer = new DispatcherTimer();
                _scanTimer.Tick += _scanTimer_Tick;

                double switchScanSpeed = (double)SwitchScanSpeedComboBox.SelectedValue;
                int seconds = (int)switchScanSpeed;
                int milliseconds = (int)((switchScanSpeed - seconds) * 1000);
                _scanTimer.Interval = new TimeSpan(0, 0, 0, seconds, milliseconds);
            }

            HandleScanTimerTick();

            _scanTimer.Start();
        }

        private void StopScan()
        {
            if (_scanTimer != null)
            {
                _scanTimer.Stop();
            }

            if (_subScanElementHighlighted != null)
            {
                if ((panelHighlighted != null) && (panelHighlighted.Name == "CardPilesPanel"))
                {
                    _subScanElementHighlighted.BorderBrush = null;
                }
                else
                {
                    _subScanElementHighlighted.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
                    _subScanElementHighlighted.BorderThickness = new Thickness(1);
                }

                _subScanElementHighlighted = null;
            }

            if (panelHighlighted != null)
            {
                panelHighlighted.Background = null;

                panelHighlighted = null;
            }

            _scanLoopCount = 0;
            _subScanLoopCount = 0;
            _subScanIndexHighlight = 0;

            _scanAtListIndex = 0;
            _firstScanAtListIndex = 0;

            _scanInProgress = false;
            _subScanOn = false;
            _scanWithinCardPile = false;

            _scanningWithinSwitchControlPanel = false;

            if (_cardBeingScanned != null)
            {
                _cardBeingScanned.IsScanned = false;
            }

            _cardBeingScanned = null;
        }

        private void _scanTimer_Tick(object sender, object e)
        {
            if (_inScanTimer)
            {
                return;
            }

            _inScanTimer = true;

            HandleScanTimerTick();

            _inScanTimer = false;
        }

        private void HandleSwitchPress()
        {
            Control focusedControl = FocusManager.GetFocusedElement() as Control;

            if (!this._scanInProgress)
            {
                GameAppBar.IsOpen = false;

                StartScan();

                _scanInProgress = true;
            }
            else if (!_subScanOn)
            {
                if (panelHighlighted.Name != "ScanModeRestartPanel")
                {
                    _subScanOn = true;
                }
                else
                {
                    if (!_scanningWithinSwitchControlPanel)
                    {
                        _scanningWithinSwitchControlPanel = true;
                
                        _scanLoopCount = 0;
                    }
                    else
                    {
                        Button focusedElement = FocusManager.GetFocusedElement() as Button;

                        NextCardDeck.Focus(FocusState.Programmatic);

                        if (focusedElement == SwitchModeRestartButton)
                        {
                            StopScan();

                            ScanModeRestartButton_Click(null, null);
                        }
                        else
                        {
                            GameAppBar.IsOpen = true;
                        }

                        return;
                    }
                }

                _scanTimer.Stop();
                _scanTimer.Start();

                HandleScanTimerTick();
            }
            else
            {
                int cardSelectedIndex = -1;

                if (_scanWithinCardPile)
                {
                    cardSelectedIndex = _scanAtListIndex;

                    _scanWithinCardPile = false;
                }
                else if (panelHighlighted.Name == "CardPilesPanel")
                {
                    var list = (ListView)CardPileGrid.FindName("CardPile" + _subScanIndexHighlight);

                    cardSelectedIndex = list.Items.Count - 1;

                    if (list.Items.Count > 1)
                    {
                        var items = GetListSource(list);

                        int countFaceUp = 0;
                        foreach (PlayingCard card in items)
                        {
                            if (!card.FaceDown)
                            {
                                ++countFaceUp;
                            }
                        }

                        if (countFaceUp > 1)
                        {
                            _scanAtListIndex = list.Items.Count - countFaceUp - 1;

                            _firstScanAtListIndex = _scanAtListIndex + 1;

                            _scanWithinCardPile = true;

                            _subScanLoopCount = 0;

                            _scanTimer.Stop();
                            _scanTimer.Start();

                            HandleScanTimerTick();
                        }
                    }
                }

                // Trigger action.
                if (!_scanWithinCardPile)
                {
                    TriggerScannedControlAction(cardSelectedIndex);
                }
            }
        }

        private void TriggerScannedControlAction(int index)
        {
            if (panelHighlighted.Name == "CardPilesPanel")
            {
                var list = (ListView)CardPileGrid.FindName("CardPile" + _subScanIndexHighlight);

                if (index == -1)
                {
                    index = list.Items.Count - 1;
                }

                if (list.Items.Count > 0)
                {
                    list.Focus(FocusState.Keyboard);

                    list.SelectedIndex = index;
                }

                _subScanElementHighlighted.BorderBrush = null;
            }
            else if (panelHighlighted.Name == "TopCornerPiles")
            {
                if (_subScanIndexHighlight == 1)
                {
                    NextCard_Click(null, null);
                }
                else
                {
                    CardDeckUpturned.IsChecked = !CardDeckUpturned.IsChecked;
                }

                NextCardDeck.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
                CardDeckUpturned.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
            }
            else if (panelHighlighted.Name == "TargetPiles")
            {
                ToggleButton btn = _subScanElementHighlighted as ToggleButton;
                TargetPile_Checked(btn, null);

                btn.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
            }

            StopScan();
        }

        private void HandleScanTimerTick()
        {
            if (panelHighlighted != null)
            {
                panelHighlighted.Background = null;
            }

            if (GameAppBar.IsOpen)
            {
                if (_scanLoopCount > 2)
                {
                    GameAppBar.IsOpen = false;
                }
                else
                {
                    if (SwitchScanSpeedComboBox.IsDropDownOpen)
                    {
                        int index = SwitchScanSpeedComboBox.SelectedIndex;
                        if ((index < 0) || (SwitchScanSpeedComboBox.SelectedIndex >= SwitchScanSpeedComboBox.Items.Count - 1))
                        {
                            index = 0;
                        }
                        else
                        {
                            ++index;
                        }

                        SwitchScanSpeedComboBox.SelectedIndex = index;
                    }
                    else
                    {
                        Control focusedControl = (Control)FocusManager.GetFocusedElement();

                        // Is focus on the last control in the appbar?
                        if (focusedControl == PlaySoundEffectsCheckBox)
                        {
                            // Move to the first of our appbar controls.
                            RestartGameButton.Focus(FocusState.Keyboard);

                            ++_scanLoopCount;
                        }
                        else
                        {
                            Control controlTarget = (Control)FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next);
                            controlTarget.Focus(FocusState.Keyboard);
                        }
                    }
                }

                return;
            }

            if (_subScanOn)
            {
                if (panelHighlighted.Name == "CardPilesPanel")
                {
                    if (_scanWithinCardPile)
                    {
                        if (_cardBeingScanned != null)
                        {
                            _cardBeingScanned.IsScanned = false;
                        }

                        var list = (ListView)CardPileGrid.FindName("CardPile" + _subScanIndexHighlight);

                        ++_scanAtListIndex;

                        if (_scanAtListIndex >= list.Items.Count)
                        {
                            _scanAtListIndex = _firstScanAtListIndex;

                            ++_subScanLoopCount;

                            if (_subScanLoopCount > 1)
                            {
                                _scanLoopCount = 0;
                                _subScanLoopCount = 0;

                                _scanWithinCardPile = false;

                                if (_cardBeingScanned != null)
                                {
                                    _cardBeingScanned.IsScanned = false;
                                }

                                _cardBeingScanned = null;

                                return;
                            }
                        }

                        object itemBeingScanned = list.Items.ElementAt(_scanAtListIndex);

                        _cardBeingScanned = itemBeingScanned as PlayingCard;
                        _cardBeingScanned.IsScanned = true;

                        var item = (list.ContainerFromItem(_cardBeingScanned) as ListViewItem);

                        var cardBounds = item.TransformToVisual(CardPileGrid).TransformPoint(
                            new Windows.Foundation.Point(0, 0));

                        Debug.WriteLine("Scroll to item: " + cardBounds.Y);

                        // Barker: While some of the card is brought into view, the results are not 
                        // as smooth as they need to be. So fix this.
                        CardPileGrid.ChangeView(null, cardBounds.Y, null);
                    }
                    else
                    {
                        CardPileGrid.ChangeView(null, 0, null);

                        ++_subScanIndexHighlight;

                        if (_subScanIndexHighlight > 7)
                        {
                            _subScanIndexHighlight = 1;

                            ++_subScanLoopCount;
                        }

                        if (_subScanLoopCount > 1)
                        {
                            _subScanElementHighlighted.BorderBrush = null;

                            panelHighlighted.Name = "CardPilesPanel";
                            panelHighlighted = CardPilesPanel;
                            panelHighlighted.Background = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;

                            _subScanOn = false;

                            _scanLoopCount = 0;
                            _subScanLoopCount = 0;
                            _subScanIndexHighlight = 0;

                            return;
                        }

                        if (_subScanElementHighlighted != null)
                        {
                            _subScanElementHighlighted.BorderBrush = null;
                        }

                        _subScanElementHighlighted = (Control)CardPileGrid.FindName("CardPile" + _subScanIndexHighlight);

                        _subScanElementHighlighted.BorderBrush = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;

                        var list = GetListSource((ListView)_subScanElementHighlighted);
                        if (list.Count > 0)
                        {
                            PlayingCard card = list[list.Count - 1];
                            RaiseNotificationEvent(
                                 AutomationNotificationKind.Other,
                                  AutomationNotificationProcessing.ImportantAll,
                                  card.Name, 
                                  NotificationActivityID_Default,
                                  NextCardDeck);
                        }
                    }
                }
                else if (panelHighlighted.Name == "TopCornerPiles")
                {
                    NextCardDeck.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
                    NextCardDeck.BorderThickness = new Thickness(1);

                    CardDeckUpturned.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
                    CardDeckUpturned.BorderThickness = new Thickness(1);

                    if (_subScanLoopCount > 1)
                    {
                        panelHighlighted.Name = "TopCornerPiles";
                        panelHighlighted = TopCornerPiles;
                        panelHighlighted.Background = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;

                        _subScanOn = false;

                        _scanLoopCount = 0;
                        _subScanLoopCount = 0;
                        _subScanIndexHighlight = 0;

                        return;
                    }

                    if (_subScanIndexHighlight == 0)
                    {
                        _subScanElementHighlighted = NextCardDeck;

                        _subScanIndexHighlight = 1;
                    }
                    else
                    {
                        // Only scan to the upturned pile is there is an upturned card.
                        if (_deckUpturned.Count > 0)
                        {
                            _subScanElementHighlighted = CardDeckUpturned;
                        }
                        else
                        {
                            ++_subScanLoopCount;
                        }

                        _subScanIndexHighlight = 0;

                        ++_subScanLoopCount;
                    }

                    _subScanElementHighlighted.BorderBrush = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;

                    _subScanElementHighlighted.BorderThickness = new Thickness(4);
                }
                else if (panelHighlighted.Name == "TargetPiles")
                {
                    if (_subScanElementHighlighted != null)
                    {
                        _subScanElementHighlighted.BorderBrush = Application.Current.Resources["CardBorderBrush"] as SolidColorBrush;
                        _subScanElementHighlighted.BorderThickness = new Thickness(1);
                    }

                    if (_subScanLoopCount > 1)
                    {
                        panelHighlighted.Name = "TargetPiles";
                        panelHighlighted = TargetPiles;
                        panelHighlighted.Background = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;

                        _subScanOn = false;

                        _scanLoopCount = 0;
                        _subScanLoopCount = 0;
                        _subScanIndexHighlight = 0;

                        return;
                    }

                    var resourceLoader = new ResourceLoader();

                    if (_subScanIndexHighlight == 0)
                    {
                        _subScanElementHighlighted = TargetPileC;

                        RaiseNotificationEvent(
                             AutomationNotificationKind.Other,
                              AutomationNotificationProcessing.ImportantAll,
                              resourceLoader.GetString("Clubs"),
                              NotificationActivityID_Default,
                              NextCardDeck);

                        _subScanIndexHighlight = 1;
                    }
                    else if (_subScanIndexHighlight == 1)
                    {
                        _subScanElementHighlighted = TargetPileD;

                        RaiseNotificationEvent(
                             AutomationNotificationKind.Other,
                              AutomationNotificationProcessing.ImportantAll,
                              resourceLoader.GetString("Diamonds"),
                              NotificationActivityID_Default,
                              NextCardDeck);

                        _subScanIndexHighlight = 2;
                    }
                    else if (_subScanIndexHighlight == 2)
                    {
                        _subScanElementHighlighted = TargetPileH;

                        RaiseNotificationEvent(
                             AutomationNotificationKind.Other,
                              AutomationNotificationProcessing.ImportantAll,
                              resourceLoader.GetString("Hearts"),
                              NotificationActivityID_Default,
                              NextCardDeck);

                        _subScanIndexHighlight = 3;
                    }
                    else if (_subScanIndexHighlight == 3)
                    {
                        _subScanElementHighlighted = TargetPileS;

                        _subScanIndexHighlight = 0;

                        RaiseNotificationEvent(
                             AutomationNotificationKind.Other,
                              AutomationNotificationProcessing.ImportantAll,
                              resourceLoader.GetString("Spades"),
                              NotificationActivityID_Default,
                              NextCardDeck);

                        ++_subScanLoopCount;
                    }
                }

                _subScanElementHighlighted.BorderBrush = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;

                _subScanElementHighlighted.BorderThickness = new Thickness(4);
            }
            else
            {
                if (panelHighlighted == null)
                {
                    MoveScanToRemainingCardsPile();
                }
                else if (panelHighlighted.Name == "CardPilesPanel")
                {
                    // We know the scan UI is visible, because we're scanning.
                    panelHighlighted = ScanModeRestartPanel;

                    _scanningWithinSwitchControlPanel = false;

                    RaiseNotificationEvent(
                         AutomationNotificationKind.Other,
                          AutomationNotificationProcessing.ImportantAll,
                          "Restart game",
                          NotificationActivityID_Default,
                          NextCardDeck);
                }
                else if (panelHighlighted.Name == "TopCornerPiles")
                {
                    panelHighlighted = TargetPiles;

                    RaiseNotificationEvent(
                         AutomationNotificationKind.Other,
                          AutomationNotificationProcessing.ImportantAll,
                          "Target group",
                          NotificationActivityID_Default,
                          NextCardDeck);
                }
                else if (panelHighlighted.Name == "TargetPiles")
                {
                    panelHighlighted = CardPilesPanel;

                    RaiseNotificationEvent(
                         AutomationNotificationKind.Other,
                          AutomationNotificationProcessing.ImportantAll,
                          "Cards",
                          NotificationActivityID_Default,
                          NextCardDeck);
                }
                else if (panelHighlighted.Name == "ScanModeRestartPanel")
                {
                    if (_scanningWithinSwitchControlPanel)
                    {
                        if (_scanLoopCount > 1)
                        {
                            _scanLoopCount = 0;

                            panelHighlighted = ScanModeRestartPanel;

                            NextCardDeck.Focus(FocusState.Programmatic);
                            
                            _scanningWithinSwitchControlPanel = false;
                        }
                        else
                        {
                            Button focusedElement = FocusManager.GetFocusedElement() as Button;

                            Button nextButton = SwitchModeRestartButton;

                            if (focusedElement == SwitchModeRestartButton)
                            {
                                nextButton = SwitchModeSettingsButton;

                                ++_scanLoopCount;
                            }

                            nextButton.Focus(FocusState.Keyboard);
                        }
                    }
                    else
                    {
                        MoveScanToRemainingCardsPile();
                    }
                }

                if (panelHighlighted != null)
                {
                    panelHighlighted.Background = Application.Current.Resources["CardScannedBorderBrush"] as SolidColorBrush;
                }
            }
        }

        private void MoveScanToRemainingCardsPile()
        {
            ++_scanLoopCount;

            if (_scanLoopCount > 2)
            {
                _scanTimer.Stop();

                _scanLoopCount = 0;

                panelHighlighted.Background = null;
                panelHighlighted = null;

                _scanInProgress = false;
                _subScanOn = false;

                return;
            }

            panelHighlighted = TopCornerPiles;

            RaiseNotificationEvent(
                 AutomationNotificationKind.Other,
                  AutomationNotificationProcessing.ImportantAll,
                  "Remaining cards",
                  NotificationActivityID_Default,
                  NextCardDeck);
        }
    }
}