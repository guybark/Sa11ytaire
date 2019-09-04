// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Windows.Media.SpeechSynthesis;

namespace Sol4All.Classes
{
    public class Notifications
    {
        StorageFile _fileSuccessSound;
        StorageFile _fileInvalidSound;
        SpeechSynthesizer _synth;


        public Notifications()
        {
            LoadFiles();
            _synth = new SpeechSynthesizer();
            //_synth.SynthesizeTextToStreamAsync.
        }

        private async Task LoadFiles()
        {
            _fileInvalidSound = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ThatDidntWork.m4a"));
            _fileSuccessSound = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/WellDone.m4a"));
        }

        public void PlaySound(bool success)
        {
            if (success)
            {
                //GetAndPlayStreamFromText("Excellent Move!!");
                GetAndPlayStreamFromSoundFile(_fileSuccessSound);
            }
            else
            {
                //GetAndPlayStreamFromText("Illegal action!!");
                GetAndPlayStreamFromSoundFile(_fileInvalidSound);
            }
        }

        //public void RaiseNotificationEvent(string target, UIElement cardDeck, bool outputDirectToTTS)
        public void RaiseNotificationEvent(AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string textString, string activityId, UIElement cardDeck, bool outputDirectToTTS)
        {
            // send text to synthesizer if appropriate.
            if (outputDirectToTTS)
            {
                GetAndPlayStreamFromText(textString);
            }

            FrameworkElementAutomationPeer peer = (FrameworkElementAutomationPeer)
                FrameworkElementAutomationPeer.FromElement(cardDeck);
            if (peer != null)
            {
                peer.RaiseNotificationEvent(
                    notificationKind,
                    notificationProcessing,
                    textString,
                    activityId
                    );

                Debug.WriteLine("NOTIFICATION: " + textString);
            }
        }

        private async Task GetAndPlayStreamFromSoundFile(IStorageFile mediaFile)
        {
            var stream = await mediaFile.OpenAsync(FileAccessMode.Read).AsTask();
            var mediaControl = new MediaElement();
            mediaControl.SetSource(stream, mediaFile.ContentType);
            mediaControl.Play();
        }

        private async Task GetAndPlayStreamFromText(string ttsText)
        {
            var stream = await _synth.SynthesizeTextToStreamAsync(ttsText);
            var mediaControl = new MediaElement();
            mediaControl.SetSource(stream, stream.ContentType);
            mediaControl.Play();
        }
    }
}
