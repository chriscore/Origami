using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class RegexMatchTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            if (!settings.ContainsKey("_pattern"))
            {
                throw new ArgumentException("_pattern must be provided for regex match");
            }

            var patternValue = ((JValue)settings["_pattern"]).Value.ToString();

            if (!settings.ContainsKey("_attributename"))
            {
                return Regex.Match(node.InnerText, patternValue).Groups[1].Value;
            }
            else
            {
                var aValue = (JValue)settings["_attributename"];
                var attribute = node.Attributes[aValue.Value.ToString()]?.Value;

                if (attribute == null)
                {
                    return null;
                }
                
                return Regex.Match(attribute, patternValue).Groups[1].Value;
            }
        }
    }
}
