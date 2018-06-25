using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Barcodarr.Models;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;
using AngleSharp;

namespace Barcodarr.Controllers
{
    [Produces("application/json")]
    [Route("api/barcode")]
    public class BarcodeController : Controller
    {

        [Route("{barcode}")]
        public BarcodeModel GetBarcode(String barcode)
        {
            var barcodeContext = new BarcodeContext();
            var barcodeDb = barcodeContext.Barcodes.FirstOrDefault(q => q.Barcode == barcode);

            if (barcodeDb == null)
            {
                var Urls = SearchDuckDuckGoForBarcode(barcode).Where(q => q.Href != "http://core.collectorz.com/movies").ToList();
                
                if (Urls.Count == 1)
                {
                    var doc = GetDocument(Urls[0].Href);

                    var title = (GetDomElements("meta[property=\"og:title\"]", doc)[0] as IHtmlMetaElement).Content.Replace(" on Collectorz.com Core Movies", "");

                    var barcodes = GetDomElements("td.barcode", doc);

                    foreach (var bc in barcodes)
                    {
                        var bModel = new BarcodeModel()
                        {
                            Barcode = bc.TextContent,
                            LastUpdated = DateTime.Now,
                            Title = title
                        };
                        barcodeContext.Barcodes.Add(bModel);
                        if (bModel.Barcode == barcode)
                        {
                            barcodeDb = bModel;
                        }
                    }

                }
                
            }
            barcodeContext.SaveChanges();
            return barcodeDb;
        }


        private List<IHtmlAnchorElement> SearchDuckDuckGoForBarcode(String Barcode)
        {
            var document = GetDocument($"https://duckduckgo.com/html/?q={Barcode}+site%3Acore.collectorz.com");
            var titles = GetDomElements("a.result__a", document);
            return titles.Cast<IHtmlAnchorElement>().ToList();
        }

        private IDocument GetDocument(String Url)
        {
            var requester = new AngleSharp.Network.Default.HttpRequester();
            requester.Headers["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";

            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester });
            var address = Url;
            var document = BrowsingContext.New(config).OpenAsync(address).Result;

            return document;
        }

        private List<IElement> GetDomElements(String CSSSelector, IDocument Dom)
        {
            return Dom.QuerySelectorAll(CSSSelector).ToList();
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