using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class RegexReplaceTransformation : ITransformationFromHtml
    {
        // /(?=<!--)([\s\S]*?)-->/
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            if (!settings.ContainsKey("_pattern"))
            {
                throw new ArgumentException("_pattern must be provided for regex match");
            }

            string replacement = "";
            if (settings.ContainsKey("_new"))
            {
                replacement = ((JValue) settings["_new"]).Value.ToString();
            }

            var patternValue = ((JValue)settings["_pattern"]).Value.ToString();

            if (!settings.ContainsKey("_attributename"))
            {
                return Regex.Replace(node.InnerText, patternValue, replacement);
            }
            else
            {
                var aValue = (JValue)settings["_attributename"];
                var attribute = node.Attributes[aValue.Value.ToString()]?.Value;

                if (attribute == null)
                {
                    return null;
                }

                return Regex.Replace(attribute, patternValue, replacement);
            }
        }
    }
}
