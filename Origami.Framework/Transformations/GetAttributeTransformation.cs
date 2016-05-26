using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class GetAttributeTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            if (!settings.ContainsKey("_attributename"))
            {
                throw new ArgumentException("_attributename");
            }

            var aValue = (JValue)settings["_attributename"];
            return node.Attributes[aValue.Value.ToString()]?.Value;
        }
    }
}
