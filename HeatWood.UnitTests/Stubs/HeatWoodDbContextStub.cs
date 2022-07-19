using HeatWood.Database;
using Microsoft.EntityFrameworkCore;

namespace HeatWood.UnitTests.Stubs;

public sealed class HeatWoodDbContextStub : HeatWoodDbContext
{
    public HeatWoodDbContextStub(DbContextOptions options) : base(options)
    {
        Database.EnsureDeleted();
    }
}