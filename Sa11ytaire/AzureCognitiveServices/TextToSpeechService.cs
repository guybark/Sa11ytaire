// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CognitiveServices.Speech;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace Sol4All.AzureCognitiveServices
{
    public class TTSService
    {
        private MediaPlayer mediaPlayer;

        public TTSService()
        {
            this.mediaPlayer = new MediaPlayer();
        }

        private string speechEndpointKey = 
            "<Insert your authentication key here.>";

        private string speechRegion = 
            "<Insert your region here.>";

        public async Task SpeakNow(string TextForSynthesis)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription(
                speechEndpointKey,
                speechRegion);

            try
            {
                // Creates a speech synthesizer.
                using (var synthesizer = new SpeechSynthesizer(config, null))
                {
                    // Receive a text from TextForSynthesis text box and synthesize it to speaker.
                    using (var result = await synthesizer.SpeakTextAsync(TextForSynthesis).ConfigureAwait(false))
                    {
                        // Checks result.
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            Debug.WriteLine("SpeakNow: SpeakTextAsync succeeded for \"" +
                                TextForSynthesis + "\"");

                            // Since native playback is not yet supported on UWP yet (currently only supported on 
                            // Windows /Linux Desktop), use the WinRT API to play audio here as a short term solution.
                            // Native playback support will be added in the future release.
                            using (var audioStream = AudioDataStream.FromResult(result))
                            {
                                // Save synthesized audio data as a wave file and user MediaPlayer to play it.
                                var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "outputaudio.wav");

                                await audioStream.SaveToWaveFileAsync(filePath);

                                mediaPlayer.Source = MediaSource.CreateFromStorageFile(
                                    await StorageFile.GetFileFromPathAsync(filePath));

                                mediaPlayer.Play();
                            }
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            Debug.WriteLine("SpeakNow: SpeakTextAsync canceled.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SpeakNow: " + ex.Message);
            }
        }
    }
}