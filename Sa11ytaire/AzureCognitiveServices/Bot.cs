// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Sol4All
{
    public class Sa11ytBot
    {
        private string botURI = 
            "https://directline.botframework.com/v3/directline/conversations";

        private string botSecret =
            "<Insert your bot's Direct Line channel secret here.>";

        private string botConversationId = string.Empty;
        private string botWatermark = string.Empty;

        public async void Chat(MainPage page, string question)
        {
            if (string.IsNullOrEmpty(question))
            {
                return;
            }

            try
            {
                // Try to start the conversation if we've not done so already.
                if (string.IsNullOrEmpty(botConversationId))
                {
                    await StartConversation();

                    Debug.WriteLine("Chat: Conversation ID is " + botConversationId);
                }

                if (!String.IsNullOrEmpty(botConversationId))
                {
                    // Post the question to the bot and get an answer.
                    string answer = await Converse(question);

                    // Process the results in a manner specific to the Sa11ytaire app.
                    string fromIdentifier = "first:";
                    string toIdentifier = "second:";

                    string fromEntity = string.Empty;
                    string toEntity = string.Empty;

                    int toIndex = answer.IndexOf(toIdentifier);
                    if (toIndex > 0)
                    {
                        toEntity = answer.Substring(toIndex + toIdentifier.Length).Trim();

                        answer = answer.Remove(toIndex);
                    }

                    int fromIndex = answer.IndexOf(fromIdentifier);
                    if (fromIndex > 0)
                    {
                        fromEntity = answer.Substring(fromIndex + fromIdentifier.Length).Trim();

                        answer = answer.Remove(fromIndex).TrimEnd();
                    }

                    bool foundIntent = page.ActOnIntentIfAppropriate(answer, fromEntity, toEntity);
                    if (!foundIntent)
                    {
                        page.ShowSpeechInputResponse(answer);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task StartConversation()
        {
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(botURI)
                };

                request.Headers.Add("Authorization", "Bearer " + botSecret);

                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    JsonTextReader reader = new JsonTextReader(new StringReader(json));

                    // Extract the conversionId from the bot's response.
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            if ((string)reader.Value == "conversationId")
                            {
                                reader.Read();

                                botConversationId = (string)reader.Value;

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StartConversation: " + ex.Message);
            }
        }

        private async Task<string> Converse(string question)
        {
            string answer = string.Empty;

            try
            {
                bool postedMessage = await ConversePostQuestion(question);
                if (postedMessage)
                {
                    answer = await ConverseGetAnswer();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Converse: " + ex.Message);
            }

            return answer;
        }

        private async Task<bool> ConversePostQuestion(string question)
        {
            bool postedQuestion = false;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(
                        botURI +
                        "/" +
                        botConversationId +
                        "/activities")
                };

                request.Headers.Add("Authorization", "Bearer " + botSecret);

                string modifiedQuestion = question.Replace("'", "\\'");

                string message =
                    "{ 'type': 'message', 'from': { 'id': 'player'}, 'text': '" +
                    modifiedQuestion +
                    "'}";

                request.Content = new StringContent(message, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("Converse: Posted question \"" + question + "\"");

                        postedQuestion = true;
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversePostQuestion: " + ex.Message);
            }

            return postedQuestion;
        }

        private async Task<string> ConverseGetAnswer()
        {
            string answer = string.Empty;

            try
            {
                // Using the watermark to only get messages which we've not processed before.
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(
                        botURI +
                        "/" +
                        botConversationId +
                        "/activities" +
                        (String.IsNullOrEmpty(botWatermark) ? "" : "?watermark=" + botWatermark))
                };

                request.Headers.Add("Authorization", "Bearer " + botSecret);

                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(request); // .Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        JsonTextReader reader = new JsonTextReader(new StringReader(json));

                        // The reply contains all the messages from either member 
                        // in the conversation since the previous request for a 
                        // response. Assume here that the last text in the reply 
                        // is all we're interested in. Don't bother checking that 
                        // the id associated with that text is actually the bot, 
                        // (id="Sa11y",) rather than the player, (id="Player").

                        string lastText = string.Empty;

                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                if ((string)reader.Value == "text")
                                {
                                    reader.Read();

                                    lastText = (string)reader.Value;

                                    Debug.WriteLine("Chat replied \"" + lastText + "\"");
                                }
                                else if ((string)reader.Value == "watermark")
                                {
                                    reader.Read();

                                    botWatermark = (string)reader.Value;
                                }
                            }
                        }

                        Debug.WriteLine("Chat replied last text of: \"" + lastText + "\"");

                        answer = lastText;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConverseGetAnswer: " + ex.Message);
            }

            return answer;
        }
    }
}