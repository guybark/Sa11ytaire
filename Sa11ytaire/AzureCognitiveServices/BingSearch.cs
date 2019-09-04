// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Sol4All.AzureCognitiveServices
{
    public class BingSearch
    {
        // Note that the endpoint string shown at the Bing Web Search overview page at the 
        // Azure Portal didn't work as-is in my tests. The  shown endpoint string was 
        // "https://api.cognitive.microsoft.com/bing/v7.0", but I needed to append "search" 
        // to the string in order for my call to the service to work. For example: 
        // "https://api.cognitive.microsoft.com/bing/v7.0/search".
        const string endpointURI = "https://api.cognitive.microsoft.com/bing/v7.0/search";

        // Your endpoint_key is shown at your Bing Web Search keys page at the Azure Portal.
        const string endpoint_key = "<Insert your endpoint key here>";

        public string AttemptBingWebSearch(string searchQuery)
        {
            // Construct the search request URI.
            var uriQuery = endpointURI + "?q=" + Uri.EscapeDataString(searchQuery);

            // For this experiment, only gather the the first result Name and URL. The results
            // also contain a collection of other properties, for example a snippet contained
            // on the found pages. And the results also contain multiple found pages. But a 
            // single Name and URL will do for demonstration purposes.
            string name = "";
            string url = "";

            try
            {
                // Perform the request and get a response.
                WebRequest request = HttpWebRequest.Create(uriQuery);
                request.Headers["Ocp-Apim-Subscription-Key"] = endpoint_key;
                HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
                string json = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JsonTextReader reader = new JsonTextReader(new StringReader(json));

                // Now work through the response picking out first Name and URL.
                // (Assume for now that the name and url will appear together.)
                while (reader.Read() && 
                        (String.IsNullOrEmpty(name) || (String.IsNullOrEmpty(url))))
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "name")
                        {
                            reader.Read();
                            name = (string)reader.Value;
                        }
                        else if ((string)reader.Value == "url")
                        {
                            reader.Read();
                            url = (string)reader.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return name + " " + url;
        }
    }
}
