using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PotekoMinecraftServer.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Name = Models.UserRoles.Admin, 
                NormalizedName = Models.UserRoles.Admin.ToUpper()
            });

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Name = Models.UserRoles.Player,
                NormalizedName = Models.UserRoles.Player.ToUpper()
            });
        }
    }
}
