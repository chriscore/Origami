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

        public string Name { get; set; }
        public JContainer Data { get; set; }
        public string CollectionSource { get; set; }
    }
}
