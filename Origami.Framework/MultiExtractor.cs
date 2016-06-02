using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Origami.Framework.Config;

namespace Origami.Framework
{
    public class MultiExtractor
    {
        public readonly List<Tuple<ConfigSection, StructuredDataExtractor>> configsToExtractors = new List<Tuple<ConfigSection, StructuredDataExtractor>>();

        public MultiExtractor(string configRootFolder, string configFilesPattern)
        {
            var files = Directory.GetFiles(configRootFolder, configFilesPattern);
            var regexRules = 0; // used to configure the C# regex cache size

            if (files.Length > 0)
            {
                foreach (var config in files.Select(StructuredDataConfig.ParseJsonFile).Where(config => config.UrlPatterns != null && config.UrlPatterns.Count > 0))
                {
                    regexRules += config.UrlPatterns.Count;
                    var extractor = new StructuredDataExtractor(config);
                    configsToExtractors.Add(Tuple.Create(config, extractor));
                }
            }

            // if not default regex cache size (15) needs to be made bigger
            if (regexRules > 15)
            {
                Regex.CacheSize = regexRules;
            }
        }

        public Tuple<ConfigSection, StructuredDataExtractor> FindFirstExtractor(string url)
        {
            foreach (var tuple in configsToExtractors)
            {
                if (tuple.Item1.UrlPatterns == null || tuple.Item1.UrlPatterns.Count <= 0)
                {
                    continue;
                }

                if (!tuple.Item1.UrlPatterns.Any(rule => Regex.IsMatch(url, rule)))
                {
                    continue;
                }

                var extractor = tuple;
                return extractor;
            }

            return null;
        }

        public IEnumerable<Tuple<ConfigSection, StructuredDataExtractor>> FindAllExtractors(string url)
        {
            foreach (var tuple in configsToExtractors)
            {
                if (tuple.Item1.UrlPatterns == null || tuple.Item1.UrlPatterns.Count <= 0)
                {
                    continue;
                }

                if (!tuple.Item1.UrlPatterns.Any(rule => Regex.IsMatch(url, rule)))
                {
                    continue;
                }

                var extractor = tuple;
                yield return extractor;
            }
        }

        public string ParsePage(string url, string html)
        {
            var extractor = FindFirstExtractor(url);

            if (extractor == null)
            {
                return null;
            }

            var structuredJson = extractor.Item2.Extract(html);
            var serializedJson = JsonConvert.SerializeObject(structuredJson, Formatting.Indented);

            return serializedJson;
        }

        public object Extract(string url, string html)
        {
            var extractor = FindFirstExtractor(url);
            
            return new
            {
                Name = extractor?.Item1?.ConfigName,
                Data = extractor?.Item2.Extract(html)
            };
        }

        public IEnumerable<TransformResults> ExtractAll(string url, string html, string collectionSource = "Direct")
        {
            var extractors = FindAllExtractors(url);
            return extractors.Select(extractor => new TransformResults(extractor?.Item1.ConfigName, extractor?.Item2?.Extract(html), collectionSource));
        }
    }

    public class TransformResults
    {
        public TransformResults(string name, JContainer data, string collectionSource)
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
