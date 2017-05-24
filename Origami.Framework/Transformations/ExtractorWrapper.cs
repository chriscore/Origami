using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origami.Framework.Config;

namespace Origami.Framework.Transformations
{
    public class ExtractorWrapper
    {
        public ExtractorWrapper(ConfigSection config, StructuredDataExtractor extractor)
        {
            this.Configuration = config;
            this.Extractor = extractor;
        }

        public ConfigSection Configuration { get; set; }
        public StructuredDataExtractor Extractor { get; set; }
    }
}
