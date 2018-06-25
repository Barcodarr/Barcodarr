using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Network.Default;
using Barcodarr.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nager.AmazonProductAdvertising;

namespace Barcodarr.Controllers
{
    [Produces("application/json")]
    [Route("api/amazon")]
    public class AmazonController : Controller
    {
        public Microsoft.Extensions.Configuration.IConfiguration configuration { get; set; }
        public AmazonController(Microsoft.Extensions.Configuration.IConfiguration config) => configuration = config;
        

        [Route("api/amazon/{barcode}")]
        public BarcodeModel GetBarcode(String barcode)
        {
            var barcodeContext = new BarcodeContext();
            var barcodeDb = barcodeContext.Barcodes.FirstOrDefault(q => q.Barcode == barcode);

            if (barcodeDb == null)
            {
                AmazonAuthentication amazonAuthentication = new AmazonAuthentication
                {
                    AccessKey = configuration["Amazon:AccessKey"],
                    SecretKey = configuration["Amazon:SecretKey"]
                };

                var wrapper = new AmazonWrapper(amazonAuthentication, AmazonEndpoint.UK, configuration["Amazon:associateTag"]);
                var result = wrapper.Lookup(barcode);
                if (result?.Items?.Item?.Count() > 0)
                {
                    var dt = DateTime.Parse(result.Items.Item[0].ItemAttributes.ReleaseDate);

                    barcodeDb = new BarcodeModel
                    {
                        Barcode = barcode,
                        Title = result.Items.Item[0].ItemAttributes.Title,
                        Year = dt.Year.ToString(),
                        LastUpdated = DateTime.Now
                    };
                    var dbResult = barcodeContext.Barcodes.Add(barcodeDb);
                    barcodeContext.SaveChanges();
                }
            }

            return barcodeDb;
        }


        public List<string> GetBarcode2()
        {
            var barcode = "191329028957";

            var requester = new HttpRequester();
            requester.Headers["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";

            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester });
            var address = $"https://duckduckgo.com/html/?q={barcode}+site%3Acore.collectorz.com";
            var document = BrowsingContext.New(config).OpenAsync(address).Result;
            var cellSelector = "a.result__a";
            var cells = document.QuerySelectorAll(cellSelector).OfType<IHtmlAnchorElement>();
            var titles = cells.Where(q=>q.TextContent != "No  results.").Select(m => new { m.TextContent, m.Href }).ToList();

            List<string> retList = new List<string>();

            foreach (var item in titles)
            {
                var l = GetBarcodes(item.Href);
                retList.AddRange(l);
            }

            return retList;
        }

        private List<string> GetBarcodes(String url)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var document = BrowsingContext.New(config).OpenAsync(url).Result;
            var cellSelector = "td.barcode";
            var cells = document.QuerySelectorAll(cellSelector);
            var titles = cells.Select(m => m.TextContent).ToList();
            return titles;
        }

    }
}