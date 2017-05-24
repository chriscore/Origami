using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Origami.Framework.Config;
using Origami.Framework.Transformations;

namespace Origami.Framework
{
    public class MultiExtractor
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly List<ExtractorWrapper> configsToExtractors = new List<ExtractorWrapper>();

        public MultiExtractor(string configRootFolder, string configFilesPattern)
        {
            Logger.Info($"Creating a new MultiExtractor with config: {configRootFolder} and pattern {configFilesPattern}");
            var files = Directory.GetFiles(configRootFolder, configFilesPattern, SearchOption.AllDirectories);
            var regexRules = 0; // used to configure the C# regex cache size

            Logger.Info($"{files.Length} config files found");
            if (files.Length > 0)
            {
                foreach (var config in files.Select(StructuredDataConfig.ParseJsonFile).Where(config => config.UrlPatterns != null && config.UrlPatterns.Count > 0))
                {
                    Logger.Info($"Building extractor config for {config.ConfigName}");
                    regexRules += config.UrlPatterns.Count;
                    var extractor = new StructuredDataExtractor(config);
                    configsToExtractors.Add(new ExtractorWrapper(config, extractor));
                }
            }

            // if not default regex cache size (15) needs to be made bigger
            if (regexRules > 15)
            {
                Regex.CacheSize = regexRules;
            }

            Logger.Info("Finished creating MultiExtractor");
        }

        public ExtractorWrapper FindFirstExtractor(string url)
        {
            Logger.Info($"Finding first extractor for {url}");
            foreach (var extractor in configsToExtractors)
            {
                if (extractor.Configuration.UrlPatterns == null || extractor.Configuration.UrlPatterns.Count <= 0)
                {
                    continue;
                }

                if (!extractor.Configuration.UrlPatterns.Any(rule => Regex.IsMatch(url, rule)))
                {
                    continue;
                }
                
                Logger.Info($"Extractor matched: {extractor.Configuration?.ConfigName}");
                return extractor;
            }

            return null;
        }

        public IEnumerable<ExtractorWrapper> FindAllExtractors(string url)
        {
            Logger.Info($"Finding all extractors for {url}");
            foreach (var extractor in configsToExtractors)
            {
                if (extractor.Configuration.UrlPatterns == null || extractor.Configuration.UrlPatterns.Count <= 0)
                {
                    continue;
                }

                if (!extractor.Configuration.UrlPatterns.Any(rule => Regex.IsMatch(url, rule)))
                {
                    continue;
                }
                
                Logger.Info($"Extractor matched: {extractor.Configuration?.ConfigName}");
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

            var structuredJson = extractor.Extractor.Extract(html);
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
                Name = extractor?.Configuration?.ConfigName,
                Data = extractor?.Extractor.Extract(html)
            };
        }

        public IEnumerable<TransformResult> ExtractAll(string url, string html, string collectionSource = "Direct")
        {
            Logger.Info($"Parsing page with all extractors with url {url}");
            Logger.Debug($"Html: {html}");

            var extractors = FindAllExtractors(url);
            return extractors.Select(extractor => new TransformResult(extractor?.Configuration.ConfigName, extractor?.Extractor?.Extract(html), collectionSource));
        }
    }
}
