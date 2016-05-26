using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class TotalTextLengthAboveListTransformation : ITransformationFromHtml
    {
        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            var ret = new StringBuilder();
            var foundParent = false;

            var currentNode = node;

            if (logicalParents != null && logicalParents.Count >= 2)
            {
                // skip any immediate parent because that's the list, we need the parent of the list, which is the grandparent
                var grandParentNode = logicalParents[logicalParents.Count - 2];
                var parentNode = grandParentNode;

                if (settings?["_startingXPath"] != null && ((JValue)settings["_startingXPath"]).Type == JTokenType.String)
                {
                    var startingXPath = ((JValue)settings["_startingXPath"]).ToObject<string>();

                    var nodes = parentNode.SelectNodes(startingXPath);

                    if (nodes != null && nodes.Count > 0)
                    {
                        parentNode = nodes[0];
                    }
                    else
                    {
                        return 0;
                    }
                }

                while (currentNode != null && currentNode != parentNode && !foundParent)
                {
                    var siblingText = GetTextFromSiblings(currentNode, parentNode, ref foundParent);

                    if (!string.IsNullOrEmpty(siblingText))
                    {
                        ret.Append(siblingText);
                        ret.Append(" ");
                    }

                    currentNode = currentNode.ParentNode;
                }
            }

            var text = ret.ToString().Trim();

            return text.Length;
        }

        public string GetTextFromSiblings(HtmlNode node, HtmlNode parentNode, ref bool foundParent)
        {
            var ret = new StringBuilder();

            if (node == null)
            {
                return ret.ToString().Trim();
            }

            HtmlNode sibling = null;

            do
            {
                sibling = sibling != null ? sibling.PreviousSibling : node.PreviousSibling;
                if (sibling == parentNode)
                {
                    foundParent = true;
                }

                if (sibling == null || sibling == parentNode)
                {
                    continue;
                }

                var siblingInnerText = sibling.InnerText;
                if (string.IsNullOrWhiteSpace(siblingInnerText))
                {
                    continue;
                }

                var text = HtmlEntity.DeEntitize(siblingInnerText).Trim();
                ret.Append(text);
                ret.Append(" ");
            }
            while (sibling != null);

            return ret.ToString().Trim();
        }
    }
}
