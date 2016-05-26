using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class StringReplaceTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            string oldValue = "";
            string newValue = "";

            if (!settings.ContainsKey("_oldvalue"))
            {
                throw new ArgumentException("StringReplaceTransformation failed: Missing transformation attribute: '_oldvalue'");
            }
            if (!settings.ContainsKey("_newvalue"))
            {
                throw new ArgumentException("StringReplaceTransformation failed: Missing transformation attribute: '_newvalue'");
            }

            oldValue = ((JValue)settings["_oldvalue"]).Value.ToString();
            newValue = ((JValue)settings["_newvalue"]).Value.ToString();

            if (!settings.ContainsKey("_attributename"))
            {
                return node.InnerText.Replace(oldValue, newValue);
            }

            var aValue = (JValue)settings["_attributename"];
            var attribute = node.Attributes[aValue.Value.ToString()]?.Value;

            if (string.IsNullOrEmpty(attribute))
            {
                return null;
            }

            return attribute.Replace(oldValue, newValue);
        }
    }
}
