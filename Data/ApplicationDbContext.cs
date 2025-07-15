using DGIIFacturadorLoginMVCApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DGIIFacturadorLoginMVCApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {   
         
        }

        public DbSet<FacturasDGII> FacturasDGII { get; set; }
        public DbSet<ItemFactura> ItemsFactura { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // ← ✅ Esto es obligatorio al heredar de IdentityDbContext

            modelBuilder.Entity<FacturasDGII>()
                .HasMany(f => f.Items)
                .WithOne(i => i.Factura)
                .HasForeignKey(i => i.FacturaId);
        }

    }
}
