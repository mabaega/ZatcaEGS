using Microsoft.EntityFrameworkCore;

namespace ZatcaEGS.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DeviceSetup> DeviceSetups { get; set; }
        public DbSet<ApprovedInvoice> ApprovedInvoices { get; set; }
    }
}
