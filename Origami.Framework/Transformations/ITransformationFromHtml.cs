using System.Collections.Generic;
using HtmlAgilityPack;

namespace Origami.Framework.Transformations
{
    public interface ITransformationFromHtml
    {
        object Transform(Dictionary<string, object> settings, HtmlNode node, List<HtmlNode> logicalParents);
    }
}
