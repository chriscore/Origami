using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class SplitTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var separator = ",";
            var trim = false;

            if (node == null)
            {
                return null;
            }

            var text = node.InnerText;

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (settings?["_separator"] != null && ((JValue)settings["_separator"]).Type == JTokenType.String)
                {
                    separator = settings["_separator"].ToString();
                }

                if (settings?["_trim"] != null && ((JValue)settings["_trim"]).Type == JTokenType.Boolean)
                {
                    trim = (bool)((JValue)settings["_trim"]).Value;
                }
            }

            try
            {
                var textParts = text.Split(new[] { separator }, StringSplitOptions.None);

                if (!trim)
                {
                    return new JArray(textParts);
                }

                for (var i = 0; i < textParts.Length; i++)
                {
                    textParts[i] = HtmlEntity.DeEntitize(textParts[i]).Trim();
                }

                return new JArray(textParts);
            }
            catch (ArgumentException)
            {
            }

            return null;
        }
    }
}
