using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HeatWood.Database;

public class HeatWoodDbContext : IdentityDbContext
{
    public HeatWoodDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        SeedData(builder);
    }
    
    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<IdentityUser>()
            .HasData(new IdentityUser("admin")
            {
                NormalizedUserName = "ADMIN",
                PasswordHash = "AQAAAAEAACcQAAAAED/00zz6hOrD1lc90wY369T5qsENpA2dY7u0ssFqHspjfQFYvETALqrgmdY8gtJSXA==" // pass: valid_password
            });
    }
}