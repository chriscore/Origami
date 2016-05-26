using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Origami.Framework.Transformations
{
    public class RemoveExtraWhitespaceTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var text = node?.InnerText;

            if (text == null)
            {
                return null;
            }

            text = HtmlEntity.DeEntitize(text).Trim();
            text = Regex.Replace(text, @"\s\s+", " ");
            return text;
        }
    }
}
