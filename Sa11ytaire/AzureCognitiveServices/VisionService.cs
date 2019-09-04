// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;

namespace Sol4All.AzureCognitiveServices
{
    class VisionService
    {
        private DispatcherTimer imageCaptureTimer;
        private int imageCaptureInterval = 3;
        private bool checkingForCard;
        private MainPage mainPage;

        private bool useCustomVision = true;

        // Toggle the state of image capture and recognition.
        public void ToggleStateOfImageProcessing(MainPage page)
        {
            mainPage = page;

            // If we're not already capturing images, start now.
            if (imageCaptureTimer == null)
            {
                imageCaptureTimer = new DispatcherTimer();
                imageCaptureTimer.Tick += imageCaptureTimer_Tick;
                imageCaptureTimer.Interval = new TimeSpan(0, 0, 0, imageCaptureInterval, 0);

                imageCaptureTimer.Start();

                page.SetLookingStatus("Looking...");
            }
            else
            {
                // Stop capturing images.
                imageCaptureTimer.Stop();
                imageCaptureTimer = null;

                page.SetLookingStatus("");
            }
        }

        private void imageCaptureTimer_Tick(object sender, object e)
        {
            CheckForCard();
        }

        private void CheckForCard()
        {
            // This call does not wait for the asynchronous image processing to 
            // complete. So if we're still in an ealier call, do nothing here.
            if (checkingForCard)
            {
                return;
            }

            checkingForCard = true;

            CaptureImageAndAnalyze();

            checkingForCard = false;
        }

        public async Task CaptureImageAndAnalyze()
        {
            // Get an image from the device's webcam.
            var stream = await CaptureImageToStream();
            if (stream != null)
            {
                string cardName = "";

                // Determine if a playing card is contained in the image.
                if (useCustomVision)
                {
                    cardName = AnalyzeImageCustomVision(stream);
                }
                else
                {
                    cardName = await AnalyzeImageComputerVision(stream);
                }

                // Check for the timer being cancelled while we were processing the image.
                if (imageCaptureTimer != null)
                {
                    if (!String.IsNullOrEmpty(cardName))
                    {
                        mainPage.SetLookingStatus("Looking... (Most recent card seen: " + cardName + ")");

                        // Have the name of the recognized card spoken.
                        SpeakCardName(cardName);

                        await Task.Run(async () =>
                        {
                            await mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                // Take whatever action's appropriate based on the 
                                // selection of the card, on the UI thread.
                                mainPage.SelectCardByIntent(cardName);
                            });
                        });
                    }
                    else
                    {
                        Debug.WriteLine("Image recognition found no card.");
                    }
                }
            }
        }

        private void SpeakCardName(string cardName)
        {
            var tts = new TTSService();
            tts.SpeakNow(cardName);
        }

        public async Task<Stream> CaptureImageToStream()
        {
            Stream stream = null;

            try
            {
                var settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;

                var mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(settings);

                var captureStream = new InMemoryRandomAccessStream();

                await mediaCapture.CapturePhotoToStreamAsync(
                    ImageEncodingProperties.CreateJpeg(), captureStream);

                stream = captureStream.AsStream();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Attempt to capture image failed: " + ex.Message);
            }

            return stream;
        }

        // For more details on using the Computer Vision API, visit
        // https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/quickstarts-sdk/csharp-analyze-sdk

        private const string endpointComputerVision = 
            "<Insert your Computer Vision endpoint here.>";

        private const string keyComputerVision = 
            "<Insert your Computer Vision key here.>";

        private static readonly List<VisualFeatureTypes> features =
            new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags
        };

        public async Task<string> AnalyzeImageComputerVision(Stream stream)
        {
            string cardName = "";   

            try
            {
                // Important: Make sure the stream's at the beginning.
                stream.Seek(0, SeekOrigin.Begin);

                ComputerVisionClient computerVision = new ComputerVisionClient(
                    new ApiKeyServiceClientCredentials(keyComputerVision));
                computerVision.Endpoint = endpointComputerVision;

                ImageAnalysis analysis = await computerVision.AnalyzeImageInStreamAsync(
                    stream, features);

                if (analysis.Description.Captions.Count > 0)
                {
                    cardName = analysis.Description.Captions[0].Text;

                    Debug.WriteLine(cardName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Attempt to recognize image failed: " + ex.Message);
            }

            return cardName;
        }

        private string projectId =
            "<Insert your Project Id here.>";

        private const string predictionKey =
            "<Insert your prediction key here.>";

        private const string SouthCentralUsEndpoint =
            "<Insert your endpoint here.>"; // Eg "https://southcentralus.api.cognitive.microsoft.com"

        private const string publishedModelName =
            "<Insert your model name here>";

        public string AnalyzeImageCustomVision(Stream stream)
        {
            string cardName = "";

            try
            {
                // Important: Make sure the stream's at the beginning.
                stream.Seek(0, SeekOrigin.Begin);

                CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
                {
                    ApiKey = predictionKey,
                    Endpoint = SouthCentralUsEndpoint
                };

                // Now call the Custom Vision service to get the predictions around the contents.

                var result = endpoint.DetectImage(
                    new Guid(projectId),
                    publishedModelName,
                    stream);

                double maxProbability = 0;

                // Retrieve the prediction with the highest probability.
                foreach (var c in result.Predictions)
                {
                    if (c.Probability > maxProbability)
                    {
                        maxProbability = c.Probability;

                        cardName = c.TagName;

                        Debug.WriteLine(cardName + ": " + maxProbability);
                    }
                }

                // For this experiment, we're only interested in 
                // predictions with a probability greater than 0.5.
                if (maxProbability < 0.5)
                {
                    cardName = "";
                }

                Debug.WriteLine(cardName + ": " + maxProbability);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Attempt to recognize image failed: " + ex.Message);
            }

            return cardName;
        }

        // The code below relates to experiments with leveraging the OCR functionality
        // available in the Azure Computer Vision service.

        //private string computerVisionSubscriptionKey = "";

        //public async Task<string> AnalyzeImageOCR(Stream stream)
        //{
        //    string description = "";

        //    OcrResult result;

        //    // Important: Make sure the stream's at the beginning.
        //    stream.Seek(0, SeekOrigin.Begin);

        //    try
        //    {
        //        IComputerVisionClient client = new ComputerVisionClient(
        //            new ApiKeyServiceClientCredentials(computerVisionSubscriptionKey))
        //        {
        //            Endpoint = "https://westus.api.cognitive.microsoft.com"
        //        };

        //        const bool DetectOrientation = true;

        //        result = client.RecognizePrintedTextInStreamAsync(DetectOrientation, stream).Result;

        //        for (int idxRegions = 0; idxRegions < result.Regions.Count; ++idxRegions)
        //        {
        //            IList<OcrLine> lines = result.Regions[idxRegions].Lines;

        //            for (int idxLines = 0; idxLines < lines.Count; ++idxLines)
        //            {
        //                IList<OcrWord> words = lines[idxLines].Words;

        //                for (int idxWords = 0; idxWords < words.Count; ++idxWords)
        //                {
        //                    OcrWord word = words[idxWords];

        //                    Debug.WriteLine("OCR Found: " + word.Text);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Attempt to recognize image failed: " + ex.Message);
        //    }

        //    return description;
        //}

        //public async Task<string> AnalyzeImageOCRHandwriting(Stream stream)
        //{
        //    string description = "";

        //    // Important: Make sure the stream's at the beginning.
        //    stream.Seek(0, SeekOrigin.Begin);

        //    try
        //    {
        //        IComputerVisionClient client = new ComputerVisionClient(
        //            new ApiKeyServiceClientCredentials(computerVisionSubscriptionKey))
        //        {
        //            Endpoint = "https://westus.api.cognitive.microsoft.com"
        //        };

        //        string operationLocation =
        //            client.RecognizeTextInStreamAsync(
        //                stream, TextRecognitionMode.Handwritten).Result.OperationLocation;

        //        int numberOfCharsInOperationId = 36;

        //        string operationId = operationLocation.Substring(
        //            operationLocation.Length - numberOfCharsInOperationId);

        //        TextOperationResult textResult =
        //            await client.GetTextOperationResultAsync(operationId);

        //        // Wait for the operation to complete
        //        int i = 0;
        //        int maxRetries = 10;
        //        while ((textResult.Status == TextOperationStatusCodes.Running ||
        //                textResult.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
        //        {
        //            await Task.Delay(1000);

        //            textResult = await client.GetTextOperationResultAsync(operationId);
        //        }

        //        Debug.WriteLine("Line count: " + textResult.RecognitionResult.Lines.Count);

        //        var lines = textResult.RecognitionResult.Lines;
        //        foreach (Line line in lines)
        //        {
        //            Debug.WriteLine("Line: " + line.Text);

        //            description += line.Text;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Attempt to recognize image failed: " + ex.Message);
        //    }

        //    return description;
        //}
    }
}
