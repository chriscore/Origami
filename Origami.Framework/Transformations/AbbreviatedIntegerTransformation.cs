using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace Origami.Framework.Transformations
{
    public class AbbreviatedIntegerTranformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var text = node?.InnerText;

            if (text == null)
            {
                return null;
            }

            var parts = text.Split(' ');
            foreach (var number in parts.Select(ConvertAbbreviatedNumber).Where(number => number.HasValue))
            {
                return number.Value;
            }

            return null;
        }

        // Input: "6.8k views" (string), Output: 6800 (integer)
        private static int? ConvertAbbreviatedNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var index = 0;

            do
            {
                var currentChar = text[index];

                if (currentChar != '.' && currentChar < '0' && currentChar > '9')
                {
                    break;
                }

                index++;
            }
            while (index < text.Length - 1);

            if (index == 0)
            {
                return null;
            }

            var firstPart = text.Substring(0, index);
            double number;

            if (!double.TryParse(firstPart, out number))
            {
                return null;
            }

            if (firstPart.Length < text.Length)
            {
                var secondPart = text.Substring(index).ToLower();

                switch (secondPart)
                {
                    case "k":
                        return Convert.ToInt32(number * 1000);
                    case "m":
                        return Convert.ToInt32(number * 1000 * 1000);
                    case "b":
                        return Convert.ToInt32(number * 1000 * 1000 * 1000);
                }
            }
            else
            {
                return Convert.ToInt32(number);
            }

            return null;
        }
    }
}
