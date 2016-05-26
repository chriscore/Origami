using System.Collections.Generic;
using HtmlAgilityPack;

namespace Origami.Framework.Transformations
{
    public class TrimTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var text = node?.InnerText;
            return text?.Trim();
        }
    }
}
