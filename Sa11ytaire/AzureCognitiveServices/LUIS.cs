// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json.Linq;

using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

// For details on creating a custom LUIS, visit:
// https://docs.microsoft.com/en-us/azure/cognitive-services/LUIS/luis-quickstart-intents-only
// https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-get-started-cs-get-intent
// https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-tutorial-speech-to-intent

namespace Sol4All.AzureCognitiveServices
{
    public class Sa11ytaireLUIS
    {
        // The AppId and the endpoint's URL and Key are all available
        // at the LUIS app's "Manage" pages at http://luis.ai;

        private string luisAppId = "<Insert your LUIS AppId here.>";
        private string luisEndpointKey = "<Insert your LUIS endpoint key here.>";
        private string luisEndpointURL = "<Insert your LUIS endpoint URL here.>";

        public async Task<LuisResult> GetIntent(string speechInput)
        {
            if (String.IsNullOrEmpty(speechInput))
            {
                return null;
            }

            // I've yet to hit an exception here, but wrap this in a try/catch 
            // while I get familiar with the call. For example, what will happen
            // when I've reached the limit of my free trial quota?

            LuisResult result = null;

            try
            {
                // Create a client with my free trial key.
                var client = new LUISRuntimeClient(new ApiKeyServiceClientCredentials(
                    luisEndpointKey)); // Available at the Keys and Endpoint page at luis.ai

                client.Endpoint = luisEndpointURL;

                // Now get the LUIS results from the text found from the speech.
                result = await client.Prediction.ResolveAsync(
                    luisAppId, // Available at the Application Information page at luis.ai
                    speechInput);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LUIS exception: " + ex.Message);
            }

            return result;
        }

        // The following code shows how to use an HTTP request to access the LUIS service.
        // For apps like this, it would be typical to use the LUIS SDK rather than making
        // an HTTP request.

        //private string luisEndPointQueryBase =
        //    "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/";

        //public async Task<JObject> GetIntentHttpClient(string speechInput)
        //{
        //    if (String.IsNullOrEmpty(speechInput))
        //    {
        //        return null;
        //    }

        //    var client = new HttpClient();
        //    var queryString = HttpUtility.ParseQueryString(string.Empty);

        //    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", luisEndpointKey);

        //    // The "q" parameter contains the utterance to send to LUIS.
        //    queryString["q"] = speechInput;

        //    // These optional request parameters are set to their default values.
        //    queryString["timezoneOffset"] = "0";
        //    queryString["verbose"] = "false";
        //    queryString["spellCheck"] = "false";
        //    queryString["staging"] = "false";

        //    // Build up the full Uri with query.
        //    var endpointUri = luisEndPointQueryBase + luisAppId + "?" + queryString;

        //    // Get LUIS to work its magic.
        //    var response = await client.GetAsync(endpointUri);

        //    // Return the full results to the caller, as it contains all sorts of goodness.
        //    var strResponseContent = await response.Content.ReadAsStringAsync();
        //    return JObject.Parse(strResponseContent);
        //}
    }
}
