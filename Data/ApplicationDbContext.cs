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


    }
}
