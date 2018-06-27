using Microsoft.EntityFrameworkCore;

namespace Barcodarr.Models
{
    public class GlobalDatabaseContext : DbContext
    {
        public static GlobalDatabaseContext GetDatabaseContext()
        {
            var context = new GlobalDatabaseContext();
            context.Database.EnsureCreated();
            return context;
        }

        public GlobalDatabaseContext()
        {

        }
        public DbSet<BarcodeModel> Barcodes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=barcodes.db");
        }
    }
}
