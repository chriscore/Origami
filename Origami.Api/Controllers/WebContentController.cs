using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using log4net;
using NReco.PhantomJS;
using OpenQA.Selenium;
using Origami.Api.Properties;
using Origami.Framework;
using SeleniumWebDriver;
using SeleniumWebDriver.Helpers;
using Origami.Framework;

namespace Origami.Api.Controllers
{
    public class WebContentController : ApiController
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        public object TransformUrl()
        {
            
            string text = null;

            // TODO: fix why URL Querystring parameter needs to be provided double url encoded, 
            // TODO: otherwise query string params in the url may break out.
            var queryString = Request.GetQueryNameValuePairs();
            var queryUrl = queryString.Where(a => a.Key.Equals("url", StringComparison.InvariantCultureIgnoreCase));
            if (!queryUrl.Any())
            {
                return BadRequest("Request parameter 'url' was missing");
            }
            var url = queryUrl.First().Value;

            MultiExtractor extractor = null;

            var queryExtractorName = queryString.Where(a => a.Key.Equals("extractorName", StringComparison.InvariantCultureIgnoreCase));
            if (queryExtractorName.Any())
            {
                extractor = new MultiExtractor(Settings.Default.TransformationsDirectory, $"{queryExtractorName.First().Value}.txt");
            }
            else
            {
                extractor = new MultiExtractor(Settings.Default.TransformationsDirectory, "*.txt");
            }

            Logger.Info($"Recieved request with querystring url: {url}");

            var matchingExtractors = extractor.FindAllExtractors(url).ToList();
            if (!matchingExtractors.Any())
            {
                Logger.Info($"No extractors matched for url {url}");
                return BadRequest($"Could not find any extractors configured that match url: {url}");
            }

            Logger.Info($"Matched extractors {matchingExtractors.Select(x => x.Configuration?.ConfigName)}");
            
            // If any of the extractors that are matched by the url have renderJS = true, then use
            // a browser that is capable of running JavaScript to render the DOM
            bool renderJs = matchingExtractors.Any(e => e.Configuration.RequiresJavascript);
            if (renderJs)
            {
                text = ExtractHtmlWithChrome(url);
                //text = ExtractHtmlWithPhantomJSNoWebdriver(url);
                var results = extractor.ExtractAll(url, text, "PhantomJS");
                return results;
            }
            else
            {
                text = ExtractHtmlWithWebClient(url);
                return extractor.ExtractAll(url, text, "WebClient");
            }
        }

        [HttpPost]
        public object TransformHtml()
        {
            var queryString = Request.GetQueryNameValuePairs();
            var urlQuery = queryString.Where(a => a.Key.Equals("url"));
            if (!urlQuery.Any())
            {
                return BadRequest("Request parameter 'url' was missing");
            }
            var url = urlQuery.First().Value;

            Logger.Info($"Recieved request with querystring url: {url}");

            var requestContent = Request.Content;
            var html = requestContent.ReadAsStringAsync().Result;
            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest("Request body was empty");
            }

            Logger.Info($"Recieved request with html: {html}");

            var extractor = new MultiExtractor(Settings.Default.TransformationsDirectory, "*.txt");

            var matchingExtractors = extractor.FindAllExtractors(url);
            if (!matchingExtractors.Any())
            {
                Logger.Info($"No extractors matched for url {url}");
                return BadRequest($"Could not find any extractors configured that match url: {url}");
            }

            return extractor.ExtractAll(url, html);
        }

        private static string ExtractHtmlWithWebClient(string url)
        {
            Logger.Info($"Calling {url} with WebClient");
            using (WebClient wc = new WebClient())
            {
                var text = wc.DownloadString(url);
                Logger.Debug($"Response from {url}:\r\n{text}");
                return text;
            }   
        }

        private static string ExtractHtmlWithPhantomJSNoWebdriver(string url)
        {
            Logger.Info($"Calling {url} with PhantomJS");

            string javascriptQuery = @"var page = require('webpage').create();
page.settings.userAgent = 'Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.65 Safari/537.36';
page.onError = function(msg, trace) {
  //prevent js errors from showing in page.content
  return;
};
page.open('" + url + @"', function (status) {
    console.log(page.content); //page source
    phantom.exit();
});";

            string text = "";
            using (var outputStream = new MemoryStream())
            using (var phantom = new PhantomJS())
            {
                phantom.CustomArgs = "--ssl-protocol=any --proxy-type=none";
                phantom.RunScript(
                    javascriptQuery,
                    null,
                    null,
                    outputStream
                );
                text = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
            }

            Logger.Debug($"Response from {url}:\r\n{text}");
            return text;
        }

        private static string ExtractHtmlWithPhantomJS(string url)
        {
            Logger.Info($"Calling {url} with PhantomJS");

            string text = "";
            using (var driver = WebDriver.CreatePhantomDriver())
            {
                driver.Navigate().GoToUrl(url);
                if (driver.Url == "about:blank")
                {
                    throw new WebException($"PhantomJS failed to navigate to url: {url}");
                }
                text = driver.PageSource;
            }

            Logger.Debug($"Response from {url}:\r\n{text}");

            return text;
        }

        private static string ExtractHtmlWithChrome(string url)
        {
            Logger.Info($"Calling {url} with Chrome");
            string text = "";
            using (var driver = WebDriver.CreateChromeDriver(null))
            {
                driver.Navigate().GoToUrl(url);
                if (driver.Url == "about:blank")
                {
                    throw new WebException($"Chrome failed to navigate to url: {url}");
                }
                //Thread.Sleep(500);
                text = driver.PageSource;
            }

            return text;
        }
    }
}
