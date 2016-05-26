using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Origami.Framework.Transformations
{
    public class ListTitleTransformation : ITransformationFromHtml
    {
        private static readonly HashSet<string> AllowedTags = new HashSet<string>()
        {
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "h7",
            "h8",
            "h9",
            "h10",
            "span",
            "div",
            "b",
            "em",
            "strong",
            "i",
            "p",
            "a"
        };

        public object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents)
        {
            HtmlNode sibling = null;
            var level = 0;
            var maxLevel = 3;
            var maxTitleLength = 200;

            if (settings?["_maxStepsUpward"] != null && ((JValue)settings["_maxStepsUpward"]).Type == JTokenType.Integer)
            {
                maxLevel = ((JValue)settings["_maxStepsUpward"]).ToObject<int>();
            }

            if (settings?["_maxTitleLength"] != null && ((JValue)settings["_maxTitleLength"]).Type == JTokenType.Integer)
            {
                maxTitleLength = ((JValue)settings["_maxTitleLength"]).ToObject<int>();
            }

            do
            {
                level++;
                sibling = sibling != null ? sibling.PreviousSibling : node.PreviousSibling;

                if (sibling != null && IsAllowedTypeRecursive(sibling))
                {
                    var siblingInnerText = sibling.InnerText;

                    if (!string.IsNullOrWhiteSpace(siblingInnerText))
                    {
                        var text = HtmlEntity.DeEntitize(siblingInnerText).Trim();

                        if (text.Length <= maxTitleLength)
                        {
                            return text;
                        }
                        if (text.Length > 0)
                        {
                            // stop if the first title candidate we find is not valid, but continue if the text was empty
                            return null;
                        }
                    }
                }

                // At this point the text node is empty, or a comment.
                // decrement level to ignore this node completely
                if (sibling != null && (sibling.NodeType == HtmlNodeType.Text || sibling.NodeType == HtmlNodeType.Comment))
                {
                    level--;
                }
            }
            while (sibling != null && level < maxLevel);

            return null;
        }

        private static bool IsAllowedTypeRecursive(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                return true;
            }

            if (node.NodeType != HtmlNodeType.Element)
            {
                return false;
            }

            if (!AllowedTags.Contains(node.Name))
            {
                return false;
            }

            var children = node.ChildNodes;

            return children == null || children.All(IsAllowedTypeRecursive);
        }
    }
}
