// Copyright(c) Guy Barker. All rights reserved.
// Licensed under the MIT License.

using Windows.ApplicationModel.Resources;

namespace Sol4All.Classes
{
    public sealed class Card
    {
        public Suit Suit;
        public int Rank;

        public override string ToString()
        {
            string rank;

            var resourceLoader = new ResourceLoader();

            switch (Rank)
            {
                case 1:
                    {
                        rank = resourceLoader.GetString("Ace");
                        break;
                    }
                case 11:
                    {
                        rank = resourceLoader.GetString("Jack");
                        break;
                    }
                case 12:
                    {
                        rank = resourceLoader.GetString("Queen");
                        break;
                    }
                case 13:
                    {
                        rank = resourceLoader.GetString("King");
                        break;
                    }
                default:
                    {
                        rank = resourceLoader.GetString(Rank.ToString());
                        break;
                    }
            }

            string ofText = resourceLoader.GetString("Of");
            string formattedString = "{0}" + " " + ofText + " " + "{1}";

            string suitString;

            switch (Suit)
            {
                case Suit.Clubs:
                    suitString = resourceLoader.GetString("Clubs");
                    break;
                case Suit.Diamonds:
                    suitString = resourceLoader.GetString("Diamonds");
                    break;
                case Suit.Hearts:
                    suitString = resourceLoader.GetString("Hearts");
                    break;
                default:
                    suitString = resourceLoader.GetString("Spades");
                    break;
            }

            return string.Format(formattedString, rank, suitString);
        }
    }

}
