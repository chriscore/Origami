using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Origami.Api.Properties;
using Origami.Framework;
using SeleniumWebDriver;

namespace Origami.Api.Controllers
{
    public class WebContentController : ApiController
    {
        [HttpGet]
        public object TransformUrl()
        {
            var extractor = new MultiExtractor(Settings.Default.TransformationsDirectory, "*.txt");
            string text = null;

            var queryString = Request.GetQueryNameValuePairs();
            var queryUrl = queryString.Where(a => a.Key.Equals("url"));
            if (!queryUrl.Any())
            {
                return BadRequest("Request parameter 'url' was missing");
            }
            var url = queryUrl.First().Value;

            var matchingExtractors = extractor.FindAllExtractors(url);
            if (!matchingExtractors.Any())
            {
                return BadRequest($"Could not find any extractors configured that match url: {url}");
            }

            // If any of the extractors that are matched by the url have renderJS = true, then use
            // a browser that is capable of running JavaScript to render the DOM
            bool renderJs = extractor.FindAllExtractors(url).Any(e => e.Item1.RequiresJavascript);
            if (renderJs)
            {
                text = ExtractHtmlWithPhantomJS(url);
                return extractor.ExtractAll(url, text);
            }
            else
            {
                text = ExtractHtmlWithWebClient(url);
                return extractor.ExtractAll(url, text);
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

            var requestContent = Request.Content;
            var html = requestContent.ReadAsStringAsync().Result;
            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest("Request body was empty");
            }

            var extractor = new MultiExtractor(Settings.Default.TransformationsDirectory, "*.txt");

            var matchingExtractors = extractor.FindAllExtractors(url);
            if (!matchingExtractors.Any())
            {
                return BadRequest($"Could not find any extractors configured that match url: {url}");
            }

            return extractor.ExtractAll(url, html);
        }

        private static string ExtractHtmlWithWebClient(string url)
        {
            using (WebClient wc = new WebClient())
            {
                return wc.DownloadString(url);
            }   
        }

        private static string ExtractHtmlWithPhantomJS(string url)
        {
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

            return text;
        }
    }
}
