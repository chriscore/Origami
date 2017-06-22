using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Origami.Framework
{
    public class TransformResult
    {
        public TransformResult(string name, JContainer data, string collectionSource)
        {
            this.Name = name;
            this.Data = data;
            this.CollectionSource = collectionSource;
        }

        public TransformResult(string name, string url, JContainer data, string collectionSource) : this(name, data, collectionSource)
        {
            Url = url;
        }

        public string Name { get; set; }
        public string Url { get; set; }
        public JContainer Data { get; set; }
        public string CollectionSource { get; set; }
    }
}
