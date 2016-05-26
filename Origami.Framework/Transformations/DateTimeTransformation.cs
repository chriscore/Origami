using System.Collections.Generic;
using Crisp.QA.Utilities.Helpers;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class DateTimeTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var dtHelper = new DateTimeHelper();

            if (!settings.ContainsKey("_attributename"))
            {
                return dtHelper.ParseVagueDateString(node.InnerText);
            }
            else
            {
                var aValue = (JValue) settings["_attributename"];
                var attribute = node.Attributes[aValue.Value.ToString()]?.Value;

                if (string.IsNullOrEmpty(attribute))
                {
                    return null;
                }

                return dtHelper.ParseVagueDateString(attribute);
            }
        }
    }
}
