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
        private Dictionary<string, string> currentAccessKeys = new Dictionary<string, string>();

        private void LoadAccessKeys()
        {
            currentAccessKeys.Clear();

            string accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKeyN"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "N";
            }

            currentAccessKeys.Add("N", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKeyU"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "U";
            }

            currentAccessKeys.Add("U", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKeyC"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "C";
            }

            currentAccessKeys.Add("C", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKeyD"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "D";
            }

            currentAccessKeys.Add("D", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKeyH"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "H";
            }

            currentAccessKeys.Add("H", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKeyS"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "S";
            }

            currentAccessKeys.Add("S", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey1"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "1";
            }

            currentAccessKeys.Add("1", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey2"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "2";
            }

            currentAccessKeys.Add("2", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey3"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "3";
            }

            currentAccessKeys.Add("3", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey4"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "4";
            }

            currentAccessKeys.Add("4", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey5"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "5";
            }

            currentAccessKeys.Add("5", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey6"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "6";
            }

            currentAccessKeys.Add("6", accessKey);

            accessKey = null;

            try
            {
                accessKey = (string)localSettings.Values["AccessKey7"];
            }
            catch (Exception)
            {
            }

            if (accessKey == null)
            {
                accessKey = "7";
            }

            currentAccessKeys.Add("7", accessKey);

            SetAccessKeysInUI();
        }

        private void SetAccessKeysInUI()
        {
            NextCardDeck.AccessKey = currentAccessKeys["N"];
            CardDeckUpturned.AccessKey = currentAccessKeys["U"];
            TargetPileC.AccessKey = currentAccessKeys["C"];
            TargetPileD.AccessKey = currentAccessKeys["D"];
            TargetPileH.AccessKey = currentAccessKeys["H"];
            TargetPileS.AccessKey = currentAccessKeys["S"];
            CardPile1.AccessKey = currentAccessKeys["1"];
            CardPile2.AccessKey = currentAccessKeys["2"];
            CardPile3.AccessKey = currentAccessKeys["3"];
            CardPile4.AccessKey = currentAccessKeys["4"];
            CardPile5.AccessKey = currentAccessKeys["5"];
            CardPile6.AccessKey = currentAccessKeys["6"];
            CardPile7.AccessKey = currentAccessKeys["7"];
        }
    }
}
