using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public IConfiguration Configuration { get; set; }
        public AmazonController(IConfiguration config) => Configuration = config;
        

        [Route("api/amazon/{barcode}")]
        public BarcodeModel GetBarcode(String barcode)
        {
            var barcodeContext = new GlobalDatabaseContext();
            var barcodeDb = barcodeContext.Barcodes.FirstOrDefault(q => q.Barcode == barcode);

            if (barcodeDb == null)
            {
                AmazonAuthentication amazonAuthentication = new AmazonAuthentication
                {
                    AccessKey = Configuration["Amazon:AccessKey"],
                    SecretKey = Configuration["Amazon:SecretKey"]
                };

                var wrapper = new AmazonWrapper(amazonAuthentication, AmazonEndpoint.UK, Configuration["Amazon:associateTag"]);
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

    }
}