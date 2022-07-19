using HeatWood.Entities.Blog;
using HeatWood.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HeatWood.Database;

public class HeatWoodDbContext : IdentityDbContext
{
    public DbSet<Article> Articles => Set<Article>();

    public DbSet<Category> Categories => Set<Category>();

    public HeatWoodDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Article>(entity =>
        {
            entity.Property(a => a.CreatedAt)
                .HasColumnType("date");
            entity.Property(a => a.UpdatedAt)
                .HasColumnType("date");

            entity.OwnsMany(a => a.Translations, b =>
            {
                b.ToTable("ArticleTranslations");
                b.HasKey(t => new {t.ArticleId, t.Locale});
                b.WithOwner().HasForeignKey(t => t.ArticleId);
                b.Property(t => t.Locale).HasMaxLength(2);
                b.Property(t => t.Title).HasMaxLength(Article.TitleMaxLength);
                b.Property(t => t.Slug)
                    .HasMaxLength(Slug.MaxLength)
                    .HasConversion(s => s.Value, s => new Slug(s));
                b.HasIndex(t => t.Slug)
                    .IsUnique();
            });
        });

        builder.Entity<Category>(entity =>
        {
            entity.OwnsMany(c => c.Translations, b =>
            {
                b.ToTable("CategoryTranslations");
                b.HasKey(t => new {ArticleCategoryId = t.CategoryId, t.Locale});
                b.WithOwner().HasForeignKey(t => t.CategoryId);
                b.Property(t => t.Locale).HasMaxLength(2);
                b.Property(t => t.Title).HasMaxLength(Category.TitleMaxLength);
            });
        });

        builder.Entity<ArticleCategory>(entity =>
        {
            entity.HasKey(ac => new {ac.ArticleId, ac.CategoryId});

            entity.HasOne(ac => ac.Article)
                .WithMany(a => a.ArticleCategories)
                .HasForeignKey(ac => ac.ArticleId);

            entity.HasOne(ac => ac.Category)
                .WithMany(a => a.ArticleCategories)
                .HasForeignKey(ac => ac.CategoryId);
        });

        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<IdentityUser>()
            .HasData(new IdentityUser("admin")
            {
                NormalizedUserName = "ADMIN",
                PasswordHash =
                    "AQAAAAEAACcQAAAAED/00zz6hOrD1lc90wY369T5qsENpA2dY7u0ssFqHspjfQFYvETALqrgmdY8gtJSXA==" // pass: valid_password
            });
    }
}