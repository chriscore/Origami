using System.Collections.Generic;

namespace Origami.Framework.Config
{
    public class TransformationConfig
    {
        public TransformationConfig()
        {
            Type = null;
            ConfigAttributes = new Dictionary<string, object>();
        }

        public string Type { get; set; }

        public Dictionary<string, object> ConfigAttributes { get; set; }
    }
}
