// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Sol4All.Classes;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using System.Linq;
using Windows.ApplicationModel.Resources;

using Sol4All.AzureCognitiveServices;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

namespace Sol4All
{
    public sealed partial class MainPage : Page
    {
        private bool listeningForSpeech;
        private Sa11ytaireSpeechService speechService;

        // This code can demo either continuous or one-time speech to text.
        private bool performContinuousSpeech = false;

        private Sa11ytBot bot;

        private async void AzureToggleListeningForSpeech()
        {
            if (speechService == null)
            {
                speechService = new Sa11ytaireSpeechService();
            }

            if (performContinuousSpeech && listeningForSpeech)
            {
                speechService.StopSpeechInputCustom(this);
            }
            else
            {
                listeningForSpeech = true;

                var resourceLoader = new ResourceLoader();

                // Let the player know that we're listening.
                SpeechInputStatus.Text = resourceLoader.GetString("WaitingForSpeech");

                // Get the recognized speech.

                // Note that we do not wait for the following calls to complete.

                if (performContinuousSpeech)
                {
                    speechService.StartSpeechInputCustom(this);
                }
                else
                {
                    // Perform one-off speech reco here.

                    bool usingDirectBot = ((bool)ShowBotCheckBox.IsChecked && chatDirect);

                    string speechIn = "";

                    bool useIntentRecognizer = false;

                    if (!useIntentRecognizer)
                    {
                        // This test first gets the text from the speech, and then continues on to
                        // call other services such as LUIS, QnA Maker and Search.
                        speechIn = await speechService.GetSpeechInputDefault(this, !usingDirectBot);
                    }
                    else
                    {
                        // This test uses an IntentRecognizer to get an intent and recognized speech
                        // in a single call to a service.
                        speechIn = await speechService.GetRecognizedIntent(this);
                    }

                    if (!string.IsNullOrEmpty(speechIn))
                    {
                        // Are we talking with a direct line channel bot?
                        if (usingDirectBot)
                        {
                            if (bot == null)
                            {
                                bot = new Sa11ytBot();
                            }

                            bot.Chat(this, speechIn);
                        }
                    }

                    // If the recognized speech has not yet been reported in some form,
                    // do it now.
                    if (SpeechInputStatus.Text == resourceLoader.GetString(
                        "WaitingForSpeech"))
                    {
                        SpeechInputStatus.Text =
                        " Heard: \"" + speechIn + "\"";
                    }
                }
            }
        }

        public bool ReactToSpeechInput(string speechInput, LuisResult results)
        {
            if (results == null)
            {
                return false;
            }

            bool foundIntent = false;

            string intent = "";
            string entity = "";
            string entity2 = "";

            double? score = 0;

            var resourceLoader = new ResourceLoader();

            // Do we have an intent that we feel sufficiently confident in?
            var result = results.TopScoringIntent;
            if (result != null)
            {
                score = result.Score;
                if (score > 0.4)
                {
                    // Seems good enough to me, so let's get the intent.
                    intent = result.Intent;

                    var entities = results.Entities;
                    if ((entities != null) && (entities.Count() > 0))
                    {
                        if (intent == "MoveCard")
                        {
                            if (entities.Count() >= 2)
                            {
                                for (int i = 0; i < entities.Count(); ++i)
                                {
                                    var nextEntity = entities[i];

                                    string type = nextEntity.Type;
                                    if (type.Contains("FromLocation"))
                                    {
                                        entity = nextEntity.Entity;
                                    }
                                    else if (type.Contains("ToLocation"))
                                    {
                                        entity2 = nextEntity.Entity;
                                    }
                                }
                            }
                            else
                            {
                                intent = "";

                                Debug.WriteLine("Error: MoveCard with unexpected entity count, " + entities.Count());
                            }
                        }
                        else
                        {
                            var firstEntity = entities.First<EntityModel>();

                            entity = firstEntity.Entity;
                        }
                    }
                    else
                    {
                        // If we got no entities, then this wasn't really a select or move intent.
                        if ((intent == "MoveCard") || (intent == "SelectCard"))
                        {
                            intent = "";
                        }
                    }

                    Debug.WriteLine("Score: " + score + ", Intent \"" + intent +
                        ", Entity \"" + entity + ", Entity2 \"" + entity2 + "\".");

                    string intentResults = "Heard: \"" + speechInput +
                        "\", Intent: " + intent +
                        ", Score: " + score;

                    if (performContinuousSpeech)
                    {
                        SpeechInputStatus.Text =
                            resourceLoader.GetString("WaitingForSpeech") +
                                " ( " + intentResults + ")";
                    }
                    else
                    {
                        SpeechInputStatus.Text = intentResults + ".";
                    }

                    foundIntent = ActOnIntentIfAppropriate(intent, entity, entity2);
                }
            }

            if (!foundIntent)
            {
                SpeechInputStatus.Text =
                    resourceLoader.GetString("WaitingForSpeech") +
                    " (Heard: \"" + speechInput + "\")";
            }

            return foundIntent;
        }

        public bool ActOnIntentIfAppropriate(string intent, string entity, string entity2)
        {
            bool foundIntent = true;

            switch (intent)
            {
                case "Help":

                    ReactToHelpRequest();

                    break;

                case "Utilities.Confirm":

                    // Is an app dlg up?
                    if (openDlg != null)
                    {
                        openDlg.Hide();

                        RestartGame(true);
                    }

                    break;

                case "Utilities.Cancel":

                    // Is an app dlg up?
                    if (openDlg != null)
                    {
                        openDlg.Hide();
                    }
                    else
                    {
                        // Unselect and uncheck all cards.
                        ReactToCancel();
                    }

                    break;

                case "TurnOverNextCards":

                    // Get the AutomationPeer associated with the NextCard button,
                    // and programmatically invoke the button through UIA.
                    ButtonAutomationPeer peer =
                        FrameworkElementAutomationPeer.FromElement(NextCardDeck) as ButtonAutomationPeer;
                    if (peer != null)
                    {
                        peer.Invoke();
                    }

                    break;

                case "RestartGame":

                    RestartGame(true /* screenReaderAnnouncement */);

                    break;

                case "ShowTopOfCardLists":
                case "ShowBottomOfCardLists":

                    // Bring either the top or bottom of the area containing the 
                    // dealt card list into view.
                    CardPileGrid.ChangeView(
                        null,
                        (intent == "ShowTopOfCardLists" ? 0 : CardPileGrid.ScrollableHeight),
                        null);

                    break;

                case "SelectCard":

                    SelectCardByIntent(entity);

                    break;

                case "MoveCard":

                    SelectCardByIntent(entity);

                    SelectCardByIntent(entity2);

                    break;

                default:

                    foundIntent = false;

                    break;
            }

            return foundIntent;
        }

        private void ReactToHelpRequest()
        {
            var resourceLoader = new ResourceLoader();

            // Currently only checking the top upturned card and dealt cards.

            string suggestion = "";

            if (_deckUpturned.Count > 0)
            {
                CardDeckUpturned.IsChecked = true;
            }

            if (!MoveUpturnedCardWithSingleKeyPressIfPossible(true, out suggestion))
            {
                for (int idxList = 0; idxList < cCardPiles; idxList++)
                {
                    // Check each dealt card list in turn.
                    ListView list = (ListView)CardPileGrid.FindName("CardPile" + (idxList + 1));

                    // Barker: Only checking the last card in the pile. Should really check all face-up card in the pile.

                    // All lists should have at least one item in it.

                    list.SelectedIndex = (list.Items.Count - 1);

                    if (MoveDealtCardWithSingleKeyPressIfPossible(true, out suggestion, list))
                    {
                        break;
                    }

                    list.SelectedItem = null;
                }
            }

            if (!string.IsNullOrEmpty(suggestion))
            {
                Debug.WriteLine("Announce suggestion for: " + suggestion);
                ShowSpeechInputResponse(resourceLoader.GetString("HowAboutMoving") + " " + suggestion);
            }
            else
            {
                if (_deckRemaining.Count > 0)
                {
                    suggestion = resourceLoader.GetString("HowAboutTurningOverCards");
                }
                else
                {
                    suggestion = resourceLoader.GetString("MightBeOutOfLuck");
                }

                Debug.WriteLine("Announce default suggestion of: " + suggestion);
                ShowSpeechInputResponse(suggestion);
            }
        }

        public void SpeechRecoStopped()
        {
            SpeechInputStatus.Text = "";

            listeningForSpeech = false;
        }

        private TTSService ttsService;

        public void ShowSpeechInputResponse(string speechInputResponse)
        {
            Debug.WriteLine("ShowSpeechInputResponse: " + speechInputResponse);

            // Only do this when working with the direct line channel bot.
            if ((bool)ShowBotCheckBox.IsChecked && chatDirect)
            {
                ShowNotificationsPanel.Visibility = Visibility.Visible;
                MostRecentNotificationTextBox.Text = speechInputResponse;

                if (ttsService == null)
                {
                    ttsService = new TTSService();
                }

                ttsService.SpeakNow(speechInputResponse);
            }
        }

        public void SelectCardByIntent(string entity)
        {
            bool cardIsSelected = false;

            // Is there a card upturned in the remaining card area?
            if (!cardIsSelected)
            {
                if (CardDeckUpturned.Visibility == Visibility.Visible)
                {
                    // Does the card spoken match the UIA Name of the upturned card?
                    string upturnedCardAutomationName =
                        AutomationProperties.GetName(CardDeckUpturned);
                    if (entity.ToLower() == upturnedCardAutomationName.ToLower())
                    {
                        // Check the upturned card.
                        CardDeckUpturned.IsChecked = true;

                        cardIsSelected = true;
                    }
                }
            }

            // Is the card at the top of one of the target card piles?
            if (!cardIsSelected)
            {
                string[] targetPileSuffix = { "C", "D", "H", "S" };

                for (int i = 0; i < cTargetPiles; i++)
                {
                    string targetButtonName = "TargetPile" + targetPileSuffix[i];

                    var targetButton = (ToggleButton)TargetPiles.FindName(targetButtonName);
                    string targetButtonAutomationName =
                        AutomationProperties.GetName(targetButton);

                    if (entity.ToLower() == targetButtonAutomationName.ToLower())
                    {
                        targetButton.IsChecked = true;

                        cardIsSelected = true;

                        break;
                    }
                }
            }

            if (!cardIsSelected)
            {
                // Is the card spoken available in the list of upturned dealt cards?
                for (int idxList = 0; idxList < cCardPiles; idxList++)
                {
                    // Check each dealt card list in turn.
                    ListView list = (ListView)CardPileGrid.FindName("CardPile" + (idxList + 1));

                    string listAutomationName = AutomationProperties.GetName(list);
                    if (entity.ToLower() == listAutomationName.ToLower())
                    {
                        // Select the last item in the list, which might be a card, 
                        // or it might be the slot on which a king 
                        if (list.Items.Count > 0)
                        {
                            list.SelectedIndex = list.Items.Count - 1;
                        }

                        cardIsSelected = true;

                        break;
                    }

                    for (int idxCard = 0; idxCard < list.Items.Count; idxCard++)
                    {
                        PlayingCard cardInDealtCardPile = (list.Items[idxCard] as PlayingCard);

                        // We're only interested in face-up cards here.
                        if (!cardInDealtCardPile.FaceDown)
                        {
                            string cardName = cardInDealtCardPile.Name.ToLower();
                            if (entity.ToLower() == cardName)
                            {
                                // Select the card of interest in the list.
                                list.SelectedIndex = idxCard;

                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}