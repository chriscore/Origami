using System.Collections.Generic;

namespace Origami.Framework.Config
{
    public sealed class ConfigSection
    {
        public ConfigSection()
        {
            ConfigName = string.Empty;
            RemoveTags = new HashSet<string>();
            UrlPatterns = new List<string>();
            XPathRules = new List<string>();
            Transformations = new List<TransformationConfig>();
            Children = new Dictionary<string, ConfigSection>();
            ForceArray = false;
        }

        // Friendly internal name we assign to this config
        public string ConfigName { get; set; }

        public bool RequiresJavascript { get; set; }

        // Only used in the root of the config tree
        public List<string> UrlPatterns { get; set; }

        // List of descendent tags to remove before extracting any data from this node
        public HashSet<string> RemoveTags { get; set; }

        // The list of XPaths to use to find these kinds of items in the HTML
        public List<string> XPathRules { get; set; }

        // List of transformations to run one by one in order on the extracted content
        public List<TransformationConfig> Transformations { get; set; }

        // Children attributes to extract from the HTML of this parent HTML block
        public Dictionary<string, ConfigSection> Children { get; set; }

        // Force the value of this item to be an array
        public bool ForceArray { get; set; }
    }
}
