// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Intent;
using Newtonsoft.Json;
using Windows.Media.Capture;
using Windows.UI.Core;

// For details of how to use the Azure Speech service in a C# app, visit:
// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/quickstart-csharp-dotnet-windows
// https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-recognize-speech-csharp

// Important: Update the capabilities in the app manifest to include access to the microphone.

namespace Sol4All.AzureCognitiveServices
{
    public class Sa11ytaireSpeechService
    {
        private bool microphoneAccessInitialized;
        private SpeechRecognizer recognizer;

        // Begin listening, and react to utterances. Note that
        // StartSpeechInputCustom() runs on a background thread 
        // and the main UI thread is not waiting for it to return.        
        public async Task<string> StartSpeechInputCustom(MainPage page)
        {
            string speechInput = "";

            await InitalizeMicrophone(page);

            // If we have access to the microphone, let's get some speech.
            if (microphoneAccessInitialized)
            {
                try
                {
                    var config = SpeechConfig.FromEndpoint(
                        new Uri(customSpeechServiceEndpoint),
                        customSpeechServiceSubscriptionKey);

                    var stopRecognition = new TaskCompletionSource<int>();

                    using (recognizer = new SpeechRecognizer(config))
                    {
                        // Subscribes to events.
                        recognizer.Recognizing += (s, e) =>
                        {
                            var result = e.Result;

                            //Debug.WriteLine("Speech recognizing: Recognition reason is " +
                            //    result.Reason.ToString() + ". Text is \"" +
                            //    result.Text + "\"");

                            //Debug.WriteLine(
                            //    "Duration (ms): " + result.Duration.TotalMilliseconds +
                            //    ", Offset (ticks): " + result.OffsetInTicks);
                        };

                        recognizer.Recognized += (s, e) =>
                        {
                            var result = e.Result;

                            Debug.WriteLine("Speech recognized: Recognition reason is " +
                                result.Reason.ToString() + ". Text is \"" +
                                result.Text + "\"");

                            Debug.WriteLine(
                                "Duration (ms): " + result.Duration.TotalMilliseconds +
                                ", Offset (ticks): " + result.OffsetInTicks);

                            switch (result.Reason)
                            {
                                case ResultReason.RecognizedSpeech:

                                    HandleRecognizedSpeech(result, page, stopRecognition);

                                    break;

                                case ResultReason.NoMatch:

                                    var details = NoMatchDetails.FromResult(result);

                                    Debug.WriteLine("NoMatchDetails: " + details.Reason);

                                    break;

                                default:

                                    break;
                            }
                        };

                        recognizer.SessionStopped += (s, e) =>
                        {
                            Debug.WriteLine("Custom recognizer: Session stopped.");

                            stopRecognition.TrySetResult(0);
                        };

                        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() 
                        // to stop recognition.
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                        // Waits for completion.
                        Task.WaitAny(new[] { stopRecognition.Task });

                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                        // Update the visuals in the UI to convey the fact that we're 
                        // no longer listening.
                        await Task.Run(async () =>
                        {
                            await page.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                page.SpeechRecoStopped();
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Continuous speech failed: " + ex.Message);
                }
            }

            return speechInput;
        }

        private async Task InitalizeMicrophone(MainPage page)
        {
            // Initialize the microphone access if we've not already done so.
            if (!microphoneAccessInitialized)
            {
                try
                {
                    // We need access to audio, and not video.
                    var settings = new MediaCaptureInitializationSettings();
                    settings.StreamingCaptureMode = StreamingCaptureMode.Audio;

                    var mediaCapture = new MediaCapture();
                    await mediaCapture.InitializeAsync(settings);

                    // If we got this far, we've initialized the microphone.
                    microphoneAccessInitialized = true;
                }
                catch (Exception micException)
                {
                    Debug.WriteLine("Microphone access not initialized. " +
                        micException.Message);

                    await page.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        page.SpeechRecoStopped();
                    });
                }
            }
        }

        private void HandleRecognizedSpeech(
            SpeechRecognitionResult result,
            MainPage page,
            TaskCompletionSource<int> stopRecognition)
        {
            // Ok, we think we have a complete utterance to work with.
            Debug.WriteLine("Final text result: \"" + result.Text + "\"");

            string speechInput = result.Text;

            // If we've been told to stop listening, then stop.
            if (speechInput.ToLower().StartsWith("stop listening"))
            {
                stopRecognition.TrySetResult(0);
            }
            else
            {
                // Strip off any trailing period that the recognizer 
                // might have added.
                speechInput = speechInput.TrimEnd('.');

                // Call LUIS to get the intent of the speech.
                Debug.WriteLine("Call LUIS to get intent of: \"" + speechInput + "\"");

                AttemptEverything(page, speechInput);
            }
        }

        private async Task AttemptEverything(MainPage page, string question)
        {
            // Do LUIS before QnA here, because QnA may have default answers for some
            // intent-related utterances.

            var luisService = new Sa11ytaireLUIS();
            LuisResult results = luisService.GetIntent(question).Result;

            if (results != null)
            {
                Task.Run(async () =>
                {
                    await page.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Take whatever action's appropriate 
                        // based on the intent, on the UI thread.
                        bool foundIntent = page.ReactToSpeechInput(question, results);

                        if (!foundIntent)
                        {
                            TryQnAandSearch(page, question);
                        };
                    });
                });
            }
        }

        private async Task TryQnAandSearch(MainPage page, string question)
        {
            // Don't take action here unless we're working with the 
            // direct line channel bot.
            string answer = await AttemptQnA(question);
            if (!string.IsNullOrEmpty(answer))
            { 
                Task.Run(async () =>
                {
                    await page.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        page.ShowSpeechInputResponse(answer);
                    });
                });
            }
            else
            {
                var search = new BingSearch();
                string searchResult = search.AttemptBingWebSearch(question);

                Task.Run(async () =>
                {
                    await page.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        page.ShowSpeechInputResponse(searchResult);
                    });
                });
            }
        }

        // The host string required is shown at the QnA Maker site, included in the line:
        //    Host: https://{host}/qnamaker
        static private string host = "<Insert your host string here.>";
        static private string service = "/qnamaker";

        // The knowledge base string required is not the knowledge base name. Rather 
        // it's the big set  of numbers (with dashes) shown at the QnA Maker site, 
        // included in the line:
        //   POST /knowledgebases/{knowledgebase_id} method.
        static private string knowledgebase_id = "<Insert your knowledge base id string here.>";
        static private string method = "/knowledgebases/" + knowledgebase_id + "/generateAnswer/";

        // The endpoint key string required is shown at the QnA Maker site, 
        // included in the line:
        //    Authorization: EndpointKey {endpoint_key}
        private string endpoint_key = "<Insert your endpoint key string here.>";

        private async Task<string> AttemptQnA(string question)
        {
            var uri = host + service + method;

            // Let's say we're only interested in the first answer available.
            string questionParam =
                "{'question': '" + question + "', 'top': 1}";

            var response = await Post(uri, questionParam);

            string answer = "";

            // Note: For this experiment, make various assumptions about what data 
            // will be available, and in what order. A shipping app would process 
            // this data with greater care.

            try
            {
                JsonTextReader reader = new JsonTextReader(new StringReader(response));

                // Only consider the first answer returned.

                while (reader.Read())
                {
                    if ((string)reader.Value == "answer")
                    {
                        reader.Read();
                        answer = (string)reader.Value;

                        // For this experiment, Only consider the answer 
                        // to be good to use with a score of 50.
                        reader.Read();
                        if ((string)reader.Value == "score")
                        {
                            reader.Read();
                            double score = (double)reader.Value;
                            if (score < 50)
                            {
                                answer = "";
                            }
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return answer;
        }

        private async Task<string> Post(string uri, string body)
        {
            var client = new HttpClient();

            try
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(uri);
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", "EndpointKey " + endpoint_key);

                    var response = await client.SendAsync(request);

                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return "";
        }

        // Called by the app if the player's issued a keyboard shortcut to stop the listening.
        public void StopSpeechInputCustom(MainPage page)
        {
            if (recognizer != null)
            {
                recognizer.StopContinuousRecognitionAsync();
            }
        }

        private string speechServiceSubscriptionKey = "<Insert Speech service key here.>";
        private string speechServiceRegion = "<Insert Speech service region here, eg westus>";

        // Following the custom speech model changing and being published, 
        // the endpoint changes and the key stays the same.

        // A service endpoint might start with something like:
        // "wss://westus.stt.speech.microsoft.com"
        private string customSpeechServiceEndpoint = "<Insert Speech service endpoint here.>";
        private string customSpeechServiceSubscriptionKey = "<Insert custom Speech service region here, eg westus>";

        public async Task<string> GetSpeechInputDefault(MainPage page, bool useLUIS)
        {
            string speechInput = "";

            await InitalizeMicrophone(page);

            // If we have access to the microphone, let's get some speech.
            if (microphoneAccessInitialized)
            {
                // This demo code configures use of the default Speech to Text service
                // with a call to FromSubscription() passing in the service region.
                var config = SpeechConfig.FromSubscription(
                    speechServiceSubscriptionKey, speechServiceRegion);

                // This demo code configures use of the custom Speech to Text service
                // with a call to FromEndpoint() passing in the service endpoint.
                //var config = SpeechConfig.FromEndpoint(
                //    new Uri(customSpeechServiceEndpoint), customSpeechServiceSubscriptionKey);

                try
                {
                    // Create a speech recognizer using the microphone as audio input. 
                    // The default language is "en-us".
                    using (var recognizer = new SpeechRecognizer(config))
                    {
                        // The call to RecognizeAsync() will begin listening for speech, 
                        // and return when the first utterance has been recognized, (or 
                        // will time-out if no speech is heard). So it is suitable only 
                        // for single shot recognition like command or query. For long-running 
                        // recognition, use StartContinuousRecognitionAsync() instead.

                        var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                        // Have we got any recognized speech?
                        if (result.Reason == ResultReason.RecognizedSpeech)
                        {
                            speechInput = result.Text;

                            // Pass the recognized text to LUIS, QnA and Bing Search if required.
                            if (useLUIS)
                            {
                                HandleRecognizedSpeech(result, page, null);
                            }

                            // Call this to pass the recognized to TTS.
                            //var tts = new TTSService();
                            //tts.SpeakNow(speechInput);

                            Debug.WriteLine("Recognized speech: \" + speechInput + \", Duration: "
                                + result.Duration);
                        }
                        else
                        {
                            Debug.WriteLine("No recognized speech available. Reco status is: " +
                                result.Reason.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Attempt to recognize speech failed. " + ex.Message);
                }
            }

            return speechInput;
        }

        public async Task<string> GetRecognizedIntent(MainPage page)
        {
            string speechInput = "";

            try
            {
                var config = SpeechConfig.FromSubscription(
                    "<Insert LUIS key here>",
                    "<Insert LUIS region here, eg westus>");

                // Creates an intent recognizer using microphone as audio input.
                using (var recognizer = new IntentRecognizer(config))
                {
                    var model = LanguageUnderstandingModel.FromAppId("<Insert LUIS app id here>");

                    // We're interested in all intents that the LUIS app knows about. (If only a
                    // subset are of interest, instead call AddIntent() for each of those intents.)
                    recognizer.AddAllIntents(model);

                    // Wait until some speech has been uttered.
                    var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                    Debug.WriteLine("Recognized result: " + result.Reason);

                    if (result.Reason == ResultReason.RecognizedIntent)
                    {
                        speechInput = result.Text;

                        // Both the recognize speech and intent are contained in the results.
                        Debug.WriteLine("RecognizedIntent: " + 
                            "Speech: \"" + result.Text + "\", Intent: " + result.IntentId);
                    }
                    else
                    {
                        Debug.WriteLine("No recognized intent available.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Attempt to recognize intent failed. " + ex.Message);
            }

            return speechInput;
        }

        // The code below was an experiment in accessing the Custom Speech service
        // through web requests, rather than using the Custom Speech SDK.

        //private bool speechRecoInProgress = false;
        //private HttpClient httpClient;
        //private MediaCapture capture;
        //private InMemoryRandomAccessStream stream;

        //public async Task<string> ToggleCustomSpeechInput()
        //{
        //    string textStatus = "";

        //    // Are we already recording audio from the mic?
        //    if (speechRecoInProgress)
        //    {
        //        // Yes, so stop recording now.
        //        await capture.StopRecordAsync();

        //        speechRecoInProgress = false;

        //        // We now have the audio data, so perform custom reco on it.
        //        textStatus = await RecognizeSpeechAsync();
        //    }
        //    else
        //    {
        //        speechRecoInProgress = true;

        //        // Get set up for recording speech.
        //        await BeginRecordingSpeech();

        //        // Begin recording audio in a format acceptable
        //        // to the custom speech service.
        //        var profile = MediaEncodingProfile.CreateWav(
        //            AudioEncodingQuality.Low);
        //        profile.Audio.SampleRate = 16000;
        //        profile.Audio.BitsPerSample = 16;

        //        await capture.StartRecordToStreamAsync(profile, stream);

        //        // Update the app UI to reflect the fact we're listening.
        //        var resourceLoader = new ResourceLoader();
        //        textStatus = resourceLoader.GetString("WaitingForSpeech");
        //    }

        //    return textStatus;
        //}

        //// We have an audio stream by now. Perform custom speech reco on it.
        //public async Task<string> RecognizeSpeechAsync()
        //{
        //    HttpWebRequest request = null;
        //    request = (HttpWebRequest)HttpWebRequest.Create(
        //        "<The REST API URL for the custom speech endpoint, copied from the Custom Speech Portal.>");

        //    request.SendChunked = true;
        //    request.Accept = @"application/json;text/xml";
        //    request.Method = "POST";
        //    request.ProtocolVersion = HttpVersion.Version11;
        //    request.ContentType = @"audio/wav; codec=audio/pcm; samplerate=16000";
        //    request.Headers["Ocp-Apim-Subscription-Key"] = customSpeechServiceSubscriptionKey;

        //    var reader = new DataReader(stream.GetInputStreamAt(0));
        //    var bytes = new byte[stream.Size];
        //    await reader.LoadAsync((uint)stream.Size);
        //    reader.ReadBytes(bytes);

        //    Stream requestStream = request.GetRequestStream();
        //    requestStream.Write(bytes, 0, bytes.Length);

        //    requestStream.Flush();

        //    string responseString = "";

        //    try
        //    {
        //        var response = request.GetResponse();
        //        Debug.WriteLine(((HttpWebResponse)response).StatusCode);

        //        var sr = new StreamReader(response.GetResponseStream());
        //        var jsonReader = new JsonTextReader(sr);
        //        var serializer = JsonSerializer.CreateDefault();

        //        CustomRecoSpeechResult customResult = serializer.Deserialize<CustomRecoSpeechResult>(jsonReader);
        //        responseString = customResult.DisplayText;

        //        sr.Dispose();
        //        response.Dispose();

        //        Debug.WriteLine(responseString);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }

        //    return responseString;
        //}

        //private async Task<bool> BeginRecordingSpeech()
        //{
        //    if (stream != null)
        //    {
        //        stream.Dispose();
        //    }

        //    stream = new InMemoryRandomAccessStream();
        //    if (capture != null)
        //    {
        //        capture.Dispose();
        //    }

        //    try
        //    {
        //        MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
        //        {
        //            StreamingCaptureMode = StreamingCaptureMode.Audio
        //        };

        //        capture = new MediaCapture();

        //        await capture.InitializeAsync(settings);

        //        capture.RecordLimitationExceeded += (MediaCapture sender) =>
        //        {
        //            speechRecoInProgress = false;

        //            throw new Exception("Record Limitation Exceeded ");
        //        };

        //        capture.Failed += (MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs) =>
        //        {
        //            speechRecoInProgress = false;

        //            throw new Exception("Code: " + errorEventArgs.Code + ", " +
        //                errorEventArgs.Message);
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        if ((ex.InnerException != null) &&
        //            (ex.InnerException.GetType() ==
        //                typeof(UnauthorizedAccessException)))
        //        {
        //            throw ex.InnerException;
        //        }
        //        throw;
        //    }

        //    return true;
        //}
    }

    //public class CustomRecoSpeechResult
    //{
    //    public string RecognitionStatus;
    //    public string DisplayText;
    //    public string Offset;
    //    public string Duration;
    //}
}
