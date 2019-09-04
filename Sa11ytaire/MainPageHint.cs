// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Sol4All.Classes;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Sol4All
{
    public sealed partial class MainPage : Page
    {
        private void CanAMoveBeMade()
        {
            int numberOfMoves = 0;
            string moveComment = "";
            PlayingCard destinationCard = null;
            PlayingCard sourceCard = null;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            string inDealtCardPile = resourceLoader.GetString("InDealtCardPile");
            string canBeMovedToDealtCardPile = resourceLoader.GetString("CanBeMovedToDealtCardPile");
            string noMoveIsAvailable = resourceLoader.GetString("NoMoveIsAvailable");

            for (int d = 0; d < cCardPiles; d++)
            {
                ListView destinationDealtCardPile = (ListView)CardPileGrid.FindName("CardPile" + (d + 1));

                // get me top most card on this pile
                destinationCard = destinationDealtCardPile.Items[destinationDealtCardPile.Items.Count - 1] as PlayingCard;
                for (int s = 0; s < cCardPiles; s++)
                {
                    ListView sourceDealtCardPile = (ListView)CardPileGrid.FindName("CardPile" + (s + 1));
                    if (destinationDealtCardPile != sourceDealtCardPile)
                    {
                        if (sourceDealtCardPile.Items.Count == 1 && 
                            ((sourceDealtCardPile.Items[0] as PlayingCard).IsKingDropZone ||
                            (sourceDealtCardPile.Items[0] as PlayingCard).Card.Rank == 13))
                        {
                            //this is either a place holder card (KingDropZone)
                            // or the bottom most card is a King and neither should be moved.
                            continue;
                        }

                        //I need to find bottom most card that is face up
                        //because that is the one we want to see if it can be moved to the destinationCardPile
                        for (int cardsOnPile = sourceDealtCardPile.Items.Count; cardsOnPile > 0; --cardsOnPile)
                        {
                            sourceCard = sourceDealtCardPile.Items[cardsOnPile - 1] as PlayingCard;
                            if (sourceCard.CardState != CardState.FaceUp)
                            {
                                // go back and get previous seen card
                                sourceCard = sourceDealtCardPile.Items[cardsOnPile] as PlayingCard;
                                break;
                            }
                        }

                        //now let's check to see if the source card can be moved to the destination pile
                        if ((CanMoveCard(destinationCard, sourceCard)) ||
                            (destinationCard.IsKingDropZone && sourceCard.Card.Rank == 13 && 
                            (sourceDealtCardPile.Items[0] as PlayingCard).Card.Rank != 13))
                        {
                            if (numberOfMoves > 0)
                            {
                                moveComment += ", \r\n";
                            }

                            moveComment += sourceCard.Card.ToString() + 
                                " " + inDealtCardPile + " " +
                                localizedNumbers[s].ToString() +
                                " " + canBeMovedToDealtCardPile + " " +
                                localizedNumbers[d].ToString();

                            numberOfMoves++;
                        }
                    }
                }

                // now look at the upturned card pile to see if it can be moved to the destination
                // timsp: how can the below be more optimized
                /*if (_deckUpturned.Count > 0)
                {
                    sourceCard.Card = null;
                    sourceCard.CardState = CardState.FaceUp;
                    sourceCard.Card = _deckUpturned[_deckUpturned.Count - 1];

                    // below this point may need help to not repeat code
                    if (CanMoveCard(destinationCard, sourceCard))
                    {
                        if (numberOfMoves > 0)
                        {
                            moveComment += ", \r\n";
                        }
                        moveComment += sourceCard.Card.ToString() +
                            " on upturned card pile " + " can be moved to dealt card pile " + (d + 1).ToString();
                        numberOfMoves++;
                    }
                }*/
            }

            if (string.IsNullOrEmpty(moveComment))
            {
                moveComment = noMoveIsAvailable;
            }

            RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.CurrentThenMostRecent,
                moveComment,
                NotificationActivityID_Default,
                NextCardDeck);
        }

        private void LookForAHint()
        {
            if (cardHasBeenMoved && EnableAutomaticHintsCheckBox.IsChecked.Value == true)
            {
                CanAMoveBeMade();
                cardHasBeenMoved = false;
            }
        }
    }
}
