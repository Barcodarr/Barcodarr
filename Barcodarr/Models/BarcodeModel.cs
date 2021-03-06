﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Barcodarr.Models
{
    public class BarcodeModel
    {
        [Key]
        public string Barcode { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public DateTime LastUpdated { get; set; }
    }
    public class BarcodeContext : DbContext
    {
        public BarcodeContext()
        {

        }
        public DbSet<BarcodeModel> Barcodes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=barcodes.db");
        }
    }
}
