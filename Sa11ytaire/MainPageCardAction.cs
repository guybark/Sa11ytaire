// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Sol4All.AzureCognitiveServices;
using Sol4All.Classes;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Q: Where should keyboard focus be after each action?

namespace Sol4All
{
    public sealed partial class MainPage : Page
    {
        // Turn over the next card in the Remaining Card pile.
        private void NextCard_Click(object sender, RoutedEventArgs e)
        {
            DoNextCardClick();
        }

        private void DoNextCardClick()
        { 
            UncheckToggleButtons(true);

            string screenReaderAnnouncement = "";

            // Can we turn over at least one card?
            if (_deckRemaining.Count > 0)
            {
                // Yes, so how many cards can we turn over?
                int countCardsToTurn = (_deckRemaining.Count >= 3 ? 3 : _deckRemaining.Count);

                // Turn over each card in turn.
                for (int i = 0; i < countCardsToTurn; ++i)
                {
                    Card card = _deckRemaining[_deckRemaining.Count - 1];
                    _deckRemaining.Remove(card);
                    _deckUpturned.Add(card);

                    screenReaderAnnouncement += card + (i < countCardsToTurn - 1 ? ", " : " ");
                }
            }
            else
            {
                // There are no cards left to turn over, so move all the upturned cards back to 
                // the Remaining Cards pile.
                while (_deckUpturned.Count > 0)
                {
                    Card card = _deckUpturned[_deckUpturned.Count - 1];
                    _deckUpturned.Remove(card);
                    _deckRemaining.Add(card);
                }

                ClearUpturnedPileButton();
            }

            SetUpturnedCardsVisuals();

            NextCardDeck.IsEmpty = (_deckRemaining.Count == 0);

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();

            string ttsText = screenReaderAnnouncement +
                    resourceLoader.GetString("OnTop") + ". " + 
                     (_deckRemaining.Count == 0 ? resourceLoader.GetString("NoCardLeft") + ". " : "");

            RaiseNotificationEvent(
                AutomationNotificationKind.ItemAdded,
                AutomationNotificationProcessing.MostRecent,
                ttsText, 
                NotificationActivityID_Default,
                NextCardDeck);

            // If we're in the middle of a bot demo, have the text announced.
            if ((bool)ShowBotCheckBox.IsChecked && chatDirect)
            {
                if (ttsService == null)
                {
                    ttsService = new TTSService();
                }

                ttsService.SpeakNow(ttsText);
            }
        }

        private bool GameOver()
        {
            //we have moved a card to the TargetPile
            //now let's see if the game is over
            for (int i = 0; i < cTargetPiles; i++)
            {
                if (_targetPiles[i].Count != 13)
                {
                    return false;
                }
            }
            return true;
        }

        // A card in one of the Target Card piles has been checked.
        private void TargetPile_Checked(object sender, RoutedEventArgs e)
        {
            CardPileToggleButton btn = (CardPileToggleButton)sender;

            // Is the top card in the Upturned Card pile checked?
            if ((CardDeckUpturned.IsChecked.Value == true) && (_deckUpturned.Count > 0))
            {
                // Attempt to move the upturned card to the target pile.
                string suggestion;
                MoveUpturnedCardToTargetPileAsAppropriate(false, out suggestion, btn);
            }
            else
            {
                // Attempt to move a card from from of the Dealt Card piles to the Target Card pile.
                string suggestion;
                MoveDealtCardToTargetPileAsAppropriate(false, out suggestion, btn);
            }

            if (GameOver())
            {
                ShowEndOfGameDialog();
            }
        }

        // A Target Card pile button has been checked while the top card in the Upturned Card pile is checked.
        private bool MoveUpturnedCardToTargetPileAsAppropriate(bool returnSuggestion, out string suggestion, CardPileToggleButton btn)
        {
            suggestion = "";

            bool movedCard = false;

            // Clear all selection from the card pile lists.
            for (int i = 0; i < cCardPiles; i++)
            {
                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                list.SelectedItem = null;
            }

            if (_deckUpturned.Count == 0)
            {
                return false;
            }

            // cardAbove here is the upturned card.
            PlayingCard cardAbove = new PlayingCard();
            cardAbove.CardState = CardState.FaceUp;

            cardAbove.Card = _deckUpturned[_deckUpturned.Count - 1];

            if (CardDeckUpturned.IsChecked.Value == true)
            {
                // Figure out which TargetPile has been invoked.
                int index = GetTargetPileListIndex(btn);

                // No action required if the upturned card doesn;t match the suit of the TargetPile.
                if (((index == 0) && (cardAbove.Card.Suit != Suit.Clubs)) ||
                    ((index == 1) && (cardAbove.Card.Suit != Suit.Diamonds)) ||
                    ((index == 2) && (cardAbove.Card.Suit != Suit.Hearts)) ||
                    ((index == 3) && (cardAbove.Card.Suit != Suit.Spades)))
                {
                    PlaySound(false);

                    return false;
                }

                // Figure out if we should move the card.
                bool moveCard = false;

                if (cardAbove.Card.Rank == 1)
                {
                    if (_targetPiles[index].Count == 0)
                    {
                        moveCard = true;
                    }
                }
                else
                {
                    List<Card> list = _targetPiles[index];

                    try
                    {
                        if ((cardAbove.Card.Rank == list[list.Count - 1].Rank + 1) &&
                            (cardAbove.Card.Suit == list[list.Count - 1].Suit))
                        {
                            moveCard = true;
                        }
                    }
                    catch (Exception)
                    {
                        moveCard = false;
                    }
                }

                if (moveCard)
                {
                    if (returnSuggestion)
                    {
                        btn.IsChecked = true;

                        suggestion = cardAbove.Card.ToString();
                    }
                    else
                    { 
                        // Move the upturned card to the Target Pile.

                        _targetPiles[index].Add(cardAbove.Card);

                        btn.Card = cardAbove.Card;

                        //AutomationProperties.SetName(btn, (string)cardAbove.Name);

                        _deckUpturned.Remove(cardAbove.Card);

                        SetUpturnedCardsVisuals();

                        PlaySound(true);
                    }

                    movedCard = true;
                }
                else
                {
                    PlaySound(false);
                }
            }

            UncheckToggleButtons(true);

            return movedCard;
        }

        // The SetCardDetails function changes the properties set on the supplied cardDestination card.
        private void SetCardDetails(PlayingCard cardSource, PlayingCard cardDestination)
        {
            // If a cardSource card was supplied, copy important details of that card over to cardDestination.
            if (cardSource != null)
            {
                cardDestination.Card = new Card();
                cardDestination.Card.Suit = cardSource.Card.Suit;
                cardDestination.Card.Rank = cardSource.Card.Rank;

                cardDestination.CardState = CardState.FaceUp;
                cardDestination.IsKingDropZone = false;
            }
            else
            {
                // No cardSource was supplied, so effectively turn cardDestination into a empty card 
                // which becomes the drop zome for a king.

                cardDestination.Card.Suit = Suit.NoSuit;
                cardDestination.Card.Rank = 0;

                cardDestination.CardState = CardState.KingPlaceHolder;
                cardDestination.IsKingDropZone = true;

                for (int i = 0; i < cCardPiles; ++i)
                {
                    ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                    if (list.Items.Contains(cardDestination))
                    {
                        cardDestination.ListIndex = (i + 1);

                        break;
                    }
                }
            }

            // Barker todo: Take action to force a refresh of the UI. Change the PlayingCard
            // class such that this action is not necessary.
            cardDestination.FaceDown = true;
            cardDestination.FaceDown = false;
        }

        // A Target Card pile button has been checked while the top card in the Upturned Card pile is not checked,
        // so attempt to move a card from one of the Dealt Card piles.
        private bool MoveDealtCardToTargetPileAsAppropriate(bool requestSuggestion, out string suggestion, CardPileToggleButton btn)
        {
            suggestion = "";

            bool movedCard = false;

            // Determine which TargetPile has been invoked.
            int targetListIndex = GetTargetPileListIndex(btn);

            bool setButtonVisuals = false;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            string inDealtCardPile = resourceLoader.GetString("InDealtCardPile");
            string revealedString = resourceLoader.GetString("Revealed");            

            // Is anything selected in a CardPile list?
            for (int i = 0; i < cCardPiles; i++)
            {
                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                if (list.SelectedItem != null)
                {
                    // Ok, we've found a selected item in one of the Dealt Card lists.

                    PlayingCard cardAbove = (PlayingCard)list.SelectedItem;

                    string nameCardMoved = cardAbove.Name;

                    // No action if the select card's suit does not match the Target Pile.
                    if (((targetListIndex == 0) && (cardAbove.Card.Suit != Suit.Clubs)) ||
                        ((targetListIndex == 1) && (cardAbove.Card.Suit != Suit.Diamonds)) ||
                        ((targetListIndex == 2) && (cardAbove.Card.Suit != Suit.Hearts)) ||
                        ((targetListIndex == 3) && (cardAbove.Card.Suit != Suit.Spades)))
                    {
                        PlaySound(false);

                        return false;
                    }

                    string cardRevealedAnnouncement = "";

                    // Should we move an Ace?
                    if ((cardAbove.Card.Rank == 1) && (_targetPiles[targetListIndex].Count == 0))
                    {
                        if (requestSuggestion)
                        {
                            suggestion = cardAbove.Name;
                        }
                        else
                        {
                            var items = GetListSource(list);

                            // Create a new Card object for use in the target pile.
                            Card newCard = new Card();
                            newCard.Rank = cardAbove.Card.Rank;
                            newCard.Suit = cardAbove.Card.Suit;

                            _targetPiles[targetListIndex].Add(newCard);

                            setButtonVisuals = true;

                            PlaySound(true);

                            // Now take action to apparently remove the source card from the Dealt Pile list.
                            if (list.Items.Count > 1)
                            {
                                PlayingCard cardRevealed = (PlayingCard)list.Items[list.Items.Count - 2];

                                SetCardDetails(cardRevealed, cardAbove);

                                items.Remove(cardRevealed);
                            }
                            else
                            {
                                SetCardDetails(null, cardAbove);
                            }

                            cardRevealedAnnouncement = cardAbove.Name;
                        }

                        movedCard = true;
                    }
                    else if (_targetPiles[targetListIndex].Count > 0)
                    {
                        // We're not moving an Ace, and the TargetPile already contains a card.
                        Card cardBelow = (Card)_targetPiles[targetListIndex][_targetPiles[targetListIndex].Count - 1];

                        if ((cardBelow.Suit == cardAbove.Card.Suit) &&
                            (cardBelow.Rank == cardAbove.Card.Rank - 1))
                        {
                            if (requestSuggestion)
                            {
                                suggestion = cardAbove.Name;
                            }
                            else
                            {
                                var itemsRemoved = GetListSource(list);

                                // Create a new Card object for use in the target pile.
                                Card newCard = new Card();
                                newCard.Rank = cardAbove.Card.Rank;
                                newCard.Suit = cardAbove.Card.Suit;

                                _targetPiles[targetListIndex].Add(newCard);

                                // Now take action to apparently remove the source card from the Dealt Pile list.
                                if (list.Items.Count > 1)
                                {
                                    PlayingCard cardRevealed = (PlayingCard)list.Items[list.Items.Count - 2];

                                    SetCardDetails(cardRevealed, cardAbove);

                                    itemsRemoved.Remove(cardRevealed);
                                }
                                else
                                {
                                    SetCardDetails(null, cardAbove);
                                }

                                setButtonVisuals = true;

                                PlaySound(true);
                            }

                            movedCard = true;
                        }
                        else
                        {
                            // illegal move
                            // you can only put the nex sequential card on the pile
                            PlaySound(false);
                        }
                    }
                    else
                    {
                        // illegal move
                        // attempted to move a card that was not in the right order
                        PlaySound(false);
                    }

                    if (!requestSuggestion || !movedCard)
                    {
                        list.SelectedItem = null;
                    }

                    // Update the Target Pile button as appropriate.
                    if (setButtonVisuals)
                    {
                        // We know the target pile list isn't empty if we're here.
                        int count = _targetPiles[targetListIndex].Count;
                        Card card = _targetPiles[targetListIndex][count - 1];
                        btn.Card = card;

                        //AutomationProperties.SetName(btn, nameCardMoved);

                        btn.Focus(FocusState.Keyboard);

                        // Have screen readers make a related announcement.
                        string ttsText = 
                            revealedString + " " + 
                            cardRevealedAnnouncement +
                            " " + inDealtCardPile + " " +
                            localizedNumbers[i] + ".";

                        RaiseNotificationEvent(
                             AutomationNotificationKind.ItemAdded,
                             AutomationNotificationProcessing.ImportantAll,
                             ttsText, 
                             NotificationActivityID_Default,
                             NextCardDeck);
                    }

                    btn.IsChecked = false;

                    return movedCard;
                }
            }

            if (_targetPiles[targetListIndex].Count == 0)
            {
                btn.IsChecked = false;
            }

            return false;
        }

        // The selection state of one of the card in the Dealt Card piles has changed.
        private void CardPile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only take action when a card has been selected.
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            // Always deselect the target card piles.
            UncheckToggleButtons(false /* include upturned card. */);

            ListView listSelectionChanged = sender as ListView;

            // Is this an "empty" card pile?
            if (listSelectionChanged.Items.Count == 1)
            {
                if ((listSelectionChanged.Items[0] as PlayingCard).IsKingDropZone)
                {
                    EmptyCardItem_Select(listSelectionChanged);
                    LookForAHint();

                    return;
                }
            }

            // Are we trying to move the upturned card to this list?
            if (CardDeckUpturned.IsChecked.Value == true)
            {
                // cardAbove here is the upturned card.
                PlayingCard cardAbove = new PlayingCard();
                cardAbove.CardState = CardState.FaceUp;
                cardAbove.Card = _deckUpturned[_deckUpturned.Count - 1];

                // cardBelow is the selected card in the CardPile list.
                PlayingCard cardBelow = listSelectionChanged.SelectedItem as PlayingCard;
                if (CanMoveCard(cardBelow, cardAbove))
                {
                    // Move the upturned card to the CardPile list.
                    var itemsAdded = GetListSource(listSelectionChanged);
                    itemsAdded.Add(cardAbove);

                    EnableCard(cardAbove, true);

                    UncheckToggleButtons(true);

                    _deckUpturned.Remove(cardAbove.Card);

                    SetUpturnedCardsVisuals();

                    PlaySound(true);
                    cardHasBeenMoved = true;

                    listSelectionChanged.SelectedItem = null;

                    // Make sure focus is on the CardPile list.
                    FocusLastItemInList(listSelectionChanged);
                }
                else
                {
                    PlaySound(false);
                }

                LookForAHint();

                return;
            }

            // Are we trying to move a card from a Target Pile to this list?
            if (MoveTargetPileCardToCardPileAsAppropriate(listSelectionChanged))
            {
                UncheckToggleButtons(true);
                cardHasBeenMoved = true;
            }
            else
            {
                MoveCardBetweenDealtCardPiles(listSelectionChanged);
                cardHasBeenMoved = false;
            }
            LookForAHint();
        }

        private void EmptyCardItem_Select(ListView listTarget)
        {
            // Consider moving the upturned card to this Card Pile.
            if ((CardDeckUpturned.IsChecked.Value == true) && (_deckUpturned.Count > 0))
            {
                // cardAbove here will be the card being moved from the upturned card pile.
                PlayingCard cardAbove = new PlayingCard();
                cardAbove.CardState = CardState.FaceUp;
                cardAbove.Card = _deckUpturned[_deckUpturned.Count - 1];

                // Is the upturned card a King?
                if (cardAbove.Card.Rank == 13)
                {
                    var itemsAdded = GetListSource(listTarget);

                    SetCardDetails(cardAbove, itemsAdded[0]);

                    UncheckToggleButtons(true);

                    _deckUpturned.Remove(cardAbove.Card);

                    SetUpturnedCardsVisuals();

                    PlaySound(true);
                    cardHasBeenMoved = true;
                }
                else
                {
                    PlaySound(false);
                }

                FocusLastItemInList(listTarget);
                listTarget.Focus(FocusState.Keyboard);

                return;
            }

            // Is anything selected in a CardPile list?
            for (int i = 0; i < cCardPiles; i++)
            {
                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                if ((list.SelectedItem != null) && (list != listTarget))
                {
                    PlayingCard cardAbove = (PlayingCard)list.SelectedItem;
                    if (cardAbove.Card.Rank == 13)
                    {
                        // A King is selected in the other Card Pile list.
                        int movingCardIndex = list.Items.IndexOf(cardAbove);

                        // Barker: Investigate why and when this is needed.
                        // AutomationProperties.SetName(cardRevealed, (string)cardRevealed.Content);

                        PlayingCard cardRevealed = null;

                        var itemsAdded = GetListSource(listTarget);
                        var itemsRemoved = GetListSource(list);

                        // Move the King, along with any other cards above it to the empty card pile.
                        SetCardDetails(cardAbove, itemsAdded[0]);

                        var nextCardIndex = movingCardIndex;

                        // Is the king being moved the first card in that list?
                        if (movingCardIndex > 0)
                        {
                            // No, so show the card that was previously beneath the moving King.
                            cardRevealed = (PlayingCard)list.Items[movingCardIndex - 1];

                            SetCardDetails(cardRevealed, cardAbove);
                        }
                        else
                        {
                            // Effectively turn the moving card into the source list's empty item.
                            SetCardDetails(null, cardAbove);
                        }

                        // Remove an item from the source list if necessary.
                        if (cardRevealed != null)
                        {
                            itemsRemoved.Remove(cardRevealed);
                        }
                        else
                        {
                            ++nextCardIndex;
                        }

                        // Move multiple cards if necessary.
                        while (nextCardIndex < list.Items.Count)
                        {
                            var nextCard = (PlayingCard)list.Items[nextCardIndex];

                            itemsRemoved.Remove(nextCard);

                            itemsAdded.Add(nextCard);
                        }

                        list.SelectedItem = null;

                        FocusLastItemInList(listTarget);
                        listTarget.Focus(FocusState.Keyboard);

                        PlaySound(true);
                        cardHasBeenMoved = true;
                    }
                    else
                    {
                        PlaySound(false);
                    }
                }
            }
        }

        private bool MoveTargetPileCardToCardPileAsAppropriate(ListView listCardPile)
        {
            bool movedCard = false;

            CardPileToggleButton btnTargetPile = null;
            List<Card> listTargetPile = null;
            if (TargetPileC.IsChecked.Value == true)
            {
                btnTargetPile = TargetPileC;
                listTargetPile = _targetPiles[0];
            }
            else if (TargetPileD.IsChecked.Value == true)
            {
                btnTargetPile = TargetPileD;
                listTargetPile = _targetPiles[1];
            }
            else if (TargetPileH.IsChecked.Value == true)
            {
                btnTargetPile = TargetPileH;
                listTargetPile = _targetPiles[2];
            }
            else if (TargetPileS.IsChecked.Value == true)
            {
                btnTargetPile = TargetPileS;
                listTargetPile = _targetPiles[3];
            }

            if ((listTargetPile != null) && (listTargetPile.Count > 0))
            {
                PlayingCard cardAbove = new PlayingCard();
                cardAbove.CardState = CardState.FaceUp;

                cardAbove.Card = listTargetPile[listTargetPile.Count - 1];

                if (listCardPile.SelectedItem != null)
                {
                    PlayingCard cardBelow = listCardPile.SelectedItem as PlayingCard;

                    if (CanMoveCard(cardBelow, cardAbove))
                    {
                        // Move the card from the TargetPile to this CardPile list.
                        listTargetPile.Remove(cardAbove.Card);

                        var itemsAdded = GetListSource(listCardPile);
                        itemsAdded.Add(cardAbove);

                        EnableCard(cardAbove, true);

                        listCardPile.Focus(FocusState.Keyboard);

                        if (listTargetPile.Count == 0)
                        {
                            btnTargetPile.Card = null;
                        }
                        else
                        {
                            btnTargetPile.Card = listTargetPile[listTargetPile.Count - 1];
                        }

                        movedCard = true;
                    }
                }

                FocusLastItemInList(listCardPile);
            }

            return movedCard;
        }

        private void MoveCardBetweenDealtCardPiles(ListView listSelectionChanged)
        {
            bool foundOtherDealtCardPileSelected = false;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            string inDealtCardPile = resourceLoader.GetString("InDealtCardPile");
            string revealedString = resourceLoader.GetString("Revealed");

            // Is any card selected in another CardPile list?
            for (int i = 0; i < cCardPiles; i++)
            {
                ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                if (list != listSelectionChanged)
                {
                    if (list.SelectedItem != null)
                    {
                        foundOtherDealtCardPileSelected = true;

                        PlayingCard cardAbove = list.SelectedItem as PlayingCard;
                        PlayingCard cardBelow = listSelectionChanged.SelectedItem as PlayingCard;

                        if (CanMoveCard(cardBelow, cardAbove))
                        {
                            // Move the card (or cards) from the other list to this CardPile list.
                            PlayingCard cardRevealed = null;

                            int movingCardIndex = list.Items.IndexOf(cardAbove);
                            if (movingCardIndex > 0)
                            {
                                // Reveal the obscured card in the list where the card is moving from.
                                cardRevealed = (PlayingCard)list.Items[movingCardIndex - 1];
                            }

                            var itemsAdded = GetListSource(listSelectionChanged);
                            var itemsRemoved = GetListSource(list);

                            // Create a new card which will be added to the target list.
                            PlayingCard newCard = new PlayingCard();
                            newCard.Card = new Card();
                            newCard.Card.Suit = cardAbove.Card.Suit;
                            newCard.Card.Rank = cardAbove.Card.Rank;
                            newCard.CardState = CardState.FaceUp;

                            itemsAdded.Add(newCard);

                            int nextCardIndex = movingCardIndex;

                            // Was the card being moved the only item in the source list?
                            if (cardRevealed != null)
                            {
                                // No, so take action to apparently remove the card from the list.
                                SetCardDetails(cardRevealed, cardAbove);

                                itemsRemoved.Remove(cardRevealed);

                                if (cardRevealed != null)
                                {
                                    string ttsText = 
                                        revealedString + "  " + 
                                        cardAbove.Name + 
                                        " " + inDealtCardPile + " " +
                                        localizedNumbers[i] + 
                                        ".";

                                    RaiseNotificationEvent(
                                         AutomationNotificationKind.ItemAdded,
                                          AutomationNotificationProcessing.ImportantAll,
                                          ttsText, 
                                          NotificationActivityID_Default,
                                          NextCardDeck);
                                }
                            }
                            else
                            {
                                // Effectively turn the moving card into the source list's empty item.
                                SetCardDetails(null, cardAbove);

                                ++nextCardIndex;
                            }

                            // Move multiple cards if necessary.
                            while (nextCardIndex < list.Items.Count)
                            {
                                var nextCard = (PlayingCard)list.Items[nextCardIndex];

                                itemsRemoved.Remove(nextCard);

                                itemsAdded.Add(nextCard);
                            }

                            // setting the SelectItem here to null
                            // so that it doesn't trigger another pass through 
                            // CardPile_SelectionChanged as that was causing
                            // the failure sound to hit, as well as the success sound later
                            list.SelectedItem = null;
                            listSelectionChanged.SelectedItem = null;
                            FocusLastItemInList(listSelectionChanged);

                            PlaySound(true);
                            cardHasBeenMoved = true;
                        }
                        else if (!cardHasBeenMoved)
                        {
                            PlaySound(false);
                            cardHasBeenMoved = false;
                        }

                        list.SelectedItem = null;
                        listSelectionChanged.SelectedItem = null;
                    }
                }

                // When the app starts, the height of the ScrollViewer containing the dealt card piles,
                // is not always as high as expected. Until the cause of this is understood, explicitly
                // resize the UI on the first attempt to move a card here, now that all elements have 
                // their actual heights calculated. Barker: Figure out this, and remove the resize here.
                if (firstMoveToDealtCardPile)
                {
                    firstMoveToDealtCardPile = false;

                    SetCardPileSize();
                }
            }

            if (!foundOtherDealtCardPileSelected)
            {
                // A dealt card was selected, but no available move was found, and no other
                // dealt card pile was found to be selected. So check if we should move with 
                // only this card selection.
                if (this.ViewModel.SingleKeyToMove)
                {
                    string suggestion;
                    MoveDealtCardWithSingleKeyPressIfPossible(false, out suggestion, listSelectionChanged);
                }
            }
        }

        private bool _inMoveDealtCardWithSingleKeyPressIfPossible = false;

        private bool MoveDealtCardWithSingleKeyPressIfPossible(bool requestSuggestion, out string suggestion, ListView listSelectionChanged)
        {
            suggestion = "";

            bool movedCard = false;

            if (_dealingCards || _inMoveDealtCardWithSingleKeyPressIfPossible)
            {
                return false;
            }

            _inMoveDealtCardWithSingleKeyPressIfPossible = true;

            PlayingCard selectedDealtCard = null;

            if (listSelectionChanged.SelectedItem != null)
            {
                selectedDealtCard = listSelectionChanged.SelectedItem as PlayingCard;
            }
            else
            {
                selectedDealtCard = listSelectionChanged.Items[listSelectionChanged.Items.Count - 1] as PlayingCard;
            }

            bool moveCardToTargetPile = false;

            CardPileToggleButton targetCardButton = null;

            if (selectedDealtCard.Suit == Suit.Clubs)
            {
                targetCardButton = TargetPileC;
            }
            else if (selectedDealtCard.Suit == Suit.Diamonds)
            {
                targetCardButton = TargetPileD;
            }
            else if (selectedDealtCard.Suit == Suit.Hearts)
            {
                targetCardButton = TargetPileH;
            }
            else if (selectedDealtCard.Suit == Suit.Spades)
            {
                targetCardButton = TargetPileS;
            }

            if (targetCardButton != null)
            {
                if (targetCardButton.Card == null)
                {
                    moveCardToTargetPile = (selectedDealtCard.Card.Rank == 1);
                }
                else
                {
                    moveCardToTargetPile = (selectedDealtCard.Card.Rank == targetCardButton.Card.Rank + 1);
                }
            }

            if (moveCardToTargetPile)
            {
                movedCard = MoveDealtCardToTargetPileAsAppropriate(requestSuggestion, out suggestion, targetCardButton);
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
                            moveCardToDealtCardPile = (selectedDealtCard.Card.Rank == 13);
                        }
                        else
                        {
                            if (CanMoveCard(topCardInDealtCardPile, selectedDealtCard))
                            {
                                moveCardToDealtCardPile = true;
                            }
                        }

                        if (moveCardToDealtCardPile)
                        {
                            if (requestSuggestion)
                            {
                                suggestion = selectedDealtCard.Name;
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

            _inMoveDealtCardWithSingleKeyPressIfPossible = false;

            return movedCard;
        }

        private void FocusLastItemInList(ListView list)
        {
            int cItems = list.Items.Count;
            if (cItems > 0)
            {
                list.SelectedIndex = cItems - 1;
                list.SelectedItem = null;
            }
        }

        // The AccessKey for a Dealt Card pile list has been triggered. So select and focus the last 
        // item in the associated list.
        private void CardPile_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
        {
            ListView list = sender as ListView;
            if (list != null)
            {
                if (list.Items.Count > 0)
                {
                    list.Focus(FocusState.Keyboard);

                    list.SelectedIndex = list.Items.Count - 1;
                }
            }
        }

        private void PlaySound(bool success)
        {
            if (PlaySoundEffectsCheckBox.IsChecked == true)
            {
                notifications.PlaySound(success);
            }

            // If not success, clear all checked and selected elements.
            if (!success)
            {
                CardDeckUpturned.IsChecked = false;

                UncheckToggleButtons(true);

                // Clear all selection from the card pile lists.
                for (int i = 0; i < cCardPiles; i++)
                {
                    ListView list = (ListView)CardPileGrid.FindName("CardPile" + (i + 1));
                    list.SelectedItem = null;
                }
            }
        }
    }
}