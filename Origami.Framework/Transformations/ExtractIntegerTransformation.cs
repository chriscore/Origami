using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Origami.Framework.Transformations
{
    public class ExtractIntegerTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlAgilityPack.HtmlNode node, List<HtmlAgilityPack.HtmlNode> logicalParents)
        {
            var text = node?.InnerText;

            if (text == null)
            {
                return null;
            }

            var intStrMatch = Regex.Match(text, @"-?\d+");
            if (!intStrMatch.Success || string.IsNullOrEmpty(intStrMatch.Value))
            {
                return null;
            }

            int intVal;
            if (int.TryParse(intStrMatch.Value, out intVal))
            {
                return intVal;
            }

            return null;
        }
    }
}
