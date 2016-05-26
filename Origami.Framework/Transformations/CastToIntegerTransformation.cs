using System.Collections.Generic;
using HtmlAgilityPack;

namespace Origami.Framework.Transformations
{
    public class CastToIntegerTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var text = node?.InnerText;

            if (text == null)
            {
                return null;
            }

            int intVal;
            if (int.TryParse(text, out intVal))
            {
                return intVal;
            }

            return null;
        }
    }
}
