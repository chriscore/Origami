﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using log4net;
using Origami.Api.Properties;
using Origami.Framework;

namespace Origami.Api.Controllers
{
    public class ManagementController : ApiController
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string ExecutingAssemblyName
        {
            get
            {
                if (!string.IsNullOrEmpty(_executingAssemblyName)) return _executingAssemblyName;

                _executingAssemblyName = "unknown";
                var executingTitleAttribute = (AssemblyTitleAttribute)Assembly
                    .GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0];

                if (executingTitleAttribute.Title.Length > 0)
                {
                    _executingAssemblyName = executingTitleAttribute.Title;
                }

                return _executingAssemblyName;
            }

        }
        private string _executingAssemblyName;

        public string AssemblyVersion => Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString();
        
        [HttpGet]
        public object Version()
        {
            Logger.Info($"Recieved call to /Version");
            var toReturn = new { Name = ExecutingAssemblyName, Version = AssemblyVersion };
            return toReturn;
        }

        [HttpGet]
        public object ListTransforms()
        {
            Logger.Info($"Recieved call to /ListTransforms");
            var queryString = Request.GetQueryNameValuePairs();

            var q = queryString.Where(a => a.Key.Equals("filter"));
            if (!q.Any())
            {
                return GetFileContent();
            }

            var filter = q.FirstOrDefault().Value;
            if (string.IsNullOrEmpty(filter))
            {
                return BadRequest("Request parameter 'filter' was empty");
            }

            return GetFileContent(filter);
        }

        [HttpGet]
        public object ListUrlPatterns()
        {
            Logger.Info($"Recieved call to /ListUrlPatterns");
            var extractor = new MultiExtractor(Settings.Default.TransformationsDirectory, "*.txt");
            return extractor.configsToExtractors.Select(a => new { a.Configuration.ConfigName, a.Configuration.UrlPatterns });
        }
        
        [HttpPost]
        public object PostTransforms()
        {
            Logger.Info($"Recieved call to /PostTransforms");
            var queryString = Request.GetQueryNameValuePairs();

            var q = queryString.Where(a => a.Key.Equals("name"));
            if (!q.Any())
            {
                return BadRequest("Request parameter 'name' was missing");
            }

            var name = q.FirstOrDefault().Value;
            if (name.Contains('.'))
            {
                return BadRequest("Request parameter 'name' must not contain invalid file characters");
            }

            var requestContent = Request.Content;
            var rStr = requestContent.ReadAsStringAsync().Result;
            if (string.IsNullOrWhiteSpace(rStr))
            {
                return BadRequest("Request body was empty");
            }

            try
            {
                File.WriteAllText($"{Settings.Default.TransformationsDirectory}/{name}.txt", rStr);
            }
            catch (IOException e)
            {
                return InternalServerError(e);
            }
            return new { Result = true };
        }

        private IEnumerable<object> GetFileContent(string filter = "")
        {
            var di = new DirectoryInfo(Settings.Default.TransformationsDirectory);
            var files = di.EnumerateFiles($"*{filter}*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                yield return new { Name = file.Name, Contents = File.ReadAllText(file.FullName) };
            }
        }
    }
}
