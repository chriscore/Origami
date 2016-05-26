using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Origami.Framework.Config;
using Origami.Framework.Transformations;

namespace Origami.Framework
{
    public class StructuredDataExtractor
    {
        private readonly ConfigSection config;

        private readonly Dictionary<string, ITransformationFromObject> transformationsFromContainer = new Dictionary<string, ITransformationFromObject>();

        private readonly Dictionary<string, ITransformationFromHtml> transformationsFromHtml = new Dictionary<string, ITransformationFromHtml>();

        public StructuredDataExtractor(ConfigSection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;

            LoadTransformations();
        }

        public StructuredDataExtractor(string configString)
        {
            if (string.IsNullOrEmpty(configString))
            {
                throw new ArgumentNullException(nameof(configString));
            }
            config = StructuredDataConfig.ParseJsonString(configString);

            LoadTransformations();
        }

        public StructuredDataExtractor(FileInfo configFile)
        {
            if (configFile == null)
            {
                throw new ArgumentNullException(nameof(configFile));
            }

            var configString = File.ReadAllText(configFile.FullName);
            config = StructuredDataConfig.ParseJsonString(configString);

            LoadTransformations();
        }

        public JContainer Extract(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            var document = new HtmlDocument {OptionFixNestedTags = true};

            try
            {
                document.LoadHtml(html);
            }
            catch (Exception)
            {
                return null;
            }

            if (document.DocumentNode == null)
            {
                return null;
            }

            return (JContainer)Extract("root", config, document.DocumentNode, new List<HtmlNode>());
        }

        private void LoadTransformations()
        {
            var transformationFromContainerType = typeof(ITransformationFromObject);
            var transformationFromHtmlType = typeof(ITransformationFromHtml);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes;

                // http://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    assemblyTypes = ex.Types;
                }

                if (assemblyTypes == null || assemblyTypes.Length <= 0)
                {
                    continue;
                }

                var transformationFromContainers = assemblyTypes
                    .Where(x => 
                        transformationFromContainerType.IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract
                    );

                foreach (var type in transformationFromContainers)
                {
                    var transformationFromContainer = (ITransformationFromObject)Activator.CreateInstance(type);
                    transformationsFromContainer[type.Name] = transformationFromContainer;
                }

                var transformationFromHtmlTypes = assemblyTypes
                    .Where(x => 
                        transformationFromHtmlType.IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract
                    );

                foreach (var type in transformationFromHtmlTypes)
                {
                    var transformationFromHtml = (ITransformationFromHtml)Activator.CreateInstance(type);
                    transformationsFromHtml[type.Name] = transformationFromHtml;
                }
            }
        }

        private object Extract(string name, ConfigSection config, HtmlNode parentNode, List<HtmlNode> logicalParents)
        {
            RemoveUnwantedTags(config, parentNode);

            // try to extract text for this because it doesnt have children
            var containers = new JArray();

            if (config.XPathRules != null && config.XPathRules.Count > 0)
            {
                foreach (var xpath in config.XPathRules)
                {
                    // TODO: Add try catch Exception
                    var nodes = parentNode.SelectNodes(xpath);

                    if (nodes == null || nodes.Count <= 0)
                    {
                        continue;
                    }

                    var newLogicalParents = logicalParents.GetRange(0, logicalParents.Count);
                    newLogicalParents.Add(parentNode);

                    foreach (var node in nodes)
                    {
                        if (config.Children != null && config.Children.Count > 0)
                        {
                            var container = new JObject();
                            ExtractChildren(config: config, parentNode: node, container: container, logicalParents: newLogicalParents);
                            containers.Add(container);
                        }
                        else if (config.Transformations != null && config.Transformations.Count > 0)
                        {
                            var obj = RunTransformations(config.Transformations, node, newLogicalParents);

                            if (obj != null)
                            {
                                containers.Add(obj);
                            }
                        }
                        else if (node.InnerText != null)
                        {
                            containers.Add(HtmlEntity.DeEntitize(node.InnerText).Trim());
                        }
                    }
                }
            }
            else
            {
                var container = new JObject();
                ExtractChildren(config: config, parentNode: parentNode, container: container, logicalParents: logicalParents);
                containers.Add(container);
            }

            if (!config.ForceArray && containers.Count == 0)
            {
                return new JObject();
            }

            if (!config.ForceArray && containers.Count == 1)
            {
                return containers.First;
            }

            return containers;
        }

        private static void RemoveUnwantedTags(ConfigSection config, HtmlAgilityPack.HtmlNode parentNode)
        {
            if (parentNode != null && config?.RemoveTags != null && config.RemoveTags.Count > 0)
            {
                parentNode.Descendants()
                    .Where(n => config.RemoveTags.Contains(n.Name.ToLowerInvariant()))
                    .ToList()
                    .ForEach(n => n.Remove());
            }
        }

        private object RunTransformations(IEnumerable<TransformationConfig> transformations, HtmlNode node, List<HtmlNode> logicalParents)
        {
            object obj = null;

            foreach (var transformation in transformations)
            {
                var settings = transformation.ConfigAttributes;

                if (obj == null && transformationsFromHtml.ContainsKey(transformation.Type))
                {
                    obj = transformationsFromHtml[transformation.Type].Transform(settings, node, logicalParents);
                }
                else if (obj != null && transformationsFromContainer.ContainsKey(transformation.Type))
                {
                    obj = transformationsFromContainer[transformation.Type].Transform(settings, obj);
                }
                else
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Transformation chain broken at transformation type {0}", transformation.Type));
                }
            }

            return obj;
        }

        private void ExtractChildren(ConfigSection config, HtmlNode parentNode, JObject container, List<HtmlNode> logicalParents)
        {
            foreach (var child in config.Children)
            {
                var childName = child.Key;
                var childConfig = child.Value;

                var childObject = Extract(childName, childConfig, parentNode, logicalParents);

                var o = childObject as JObject;
                if (o != null)
                {
                    if (o.Count > 0)
                    {
                        container[childName] = (JToken)childObject;
                    }
                }
                else if (childObject is JArray)
                {
                    if (((JArray)childObject).Count > 0)
                    {
                        container[childName] = (JToken)childObject;
                    }
                }
                else
                {
                    container[childName] = (JToken)childObject;
                }
            }
        }
    }
}
