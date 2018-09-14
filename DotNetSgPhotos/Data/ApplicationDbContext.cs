using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DotNetSgPhotos.Models;

namespace DotNetSgPhotos.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // *** For Cosmos DB only ***
            //builder.Entity<Photo>().Metadata.CosmosSql().CollectionName = "Photos";

            // We can now define "owned" or "child" entities which group properties within other entities, 
            // very similar to how complex types used to work in EF6, but with the ability to contain reference 
            // navigation properties. In combination with table splitting, owned types allow these two entities to be 
            // automatically mapped to a single ApplicationUser table.
            //
            // References: 
            // 1. https://github.com/aspnet/EntityFrameworkCore/issues/246
            // 2. https://blogs.msdn.microsoft.com/dotnet/2017/08/14/announcing-entity-framework-core-2-0/#owned-entities-and-table-splitting
            // 3. https://github.com/aspnet/Announcements/issues/308
            builder.Entity<Photo>().OwnsMany(p => p.FacialExpressions);

            // Change the underlying type
            builder.Entity<Photo>().Property(p => p.Location).HasColumnType("Geometry");

            // *** For Cosmos DB only ***
            //builder.Entity<Photo>().Ignore(p => p.Location);
        }

        public DbSet<Photo> Photos { get; set; }
    }
}
