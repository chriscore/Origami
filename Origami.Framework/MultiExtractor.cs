using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Origami.Framework.Config;

namespace Origami.Framework
{
    public class MultiExtractor
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly List<Tuple<ConfigSection, StructuredDataExtractor>> configsToExtractors = new List<Tuple<ConfigSection, StructuredDataExtractor>>();

        public MultiExtractor(string configRootFolder, string configFilesPattern)
        {
            Logger.Info($"Creating a new MultiExtractor with config: {configRootFolder} and pattern {configFilesPattern}");
            var files = Directory.GetFiles(configRootFolder, configFilesPattern);
            var regexRules = 0; // used to configure the C# regex cache size

            Logger.Info($"{files.Length} config files found");
            if (files.Length > 0)
            {
                foreach (var config in files.Select(StructuredDataConfig.ParseJsonFile).Where(config => config.UrlPatterns != null && config.UrlPatterns.Count > 0))
                {
                    Logger.Info($"Building extractor config for {config.ConfigName}");
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

            Logger.Info("Finished creating MultiExtractor");
        }

        public Tuple<ConfigSection, StructuredDataExtractor> FindFirstExtractor(string url)
        {
            Logger.Info($"Finding first extractor for {url}");
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

                Logger.Info($"Extractor matched: {extractor.Item1?.ConfigName}");
                return extractor;
            }

            return null;
        }

        public IEnumerable<Tuple<ConfigSection, StructuredDataExtractor>> FindAllExtractors(string url)
        {
            Logger.Info($"Finding all extractors for {url}");
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
                Logger.Info($"Extractor matched: {extractor.Item1?.ConfigName}");
                yield return extractor;
            }
        }

        public string ParsePage(string url, string html)
        {
            Logger.Info($"Parsing page with first extractor with url {url}");
            Logger.Debug($"Html: {html}");

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
            Logger.Info($"Parsing page with first extractor with url {url}");
            Logger.Debug($"Html: {html}");

            var extractor = FindFirstExtractor(url);
            
            return new
            {
                Name = extractor?.Item1?.ConfigName,
                Data = extractor?.Item2.Extract(html)
            };
        }

        public IEnumerable<TransformResult> ExtractAll(string url, string html, string collectionSource = "Direct")
        {
            Logger.Info($"Parsing page with all extractors with url {url}");
            Logger.Debug($"Html: {html}");

            var extractors = FindAllExtractors(url);
            return extractors.Select(extractor => new TransformResult(extractor?.Item1.ConfigName, extractor?.Item2?.Extract(html), collectionSource));
        }
    }
}
