using KfConstructionAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KfConstructionAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Member> Members { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Member email as unique for cross-system linking
        modelBuilder.Entity<Member>()
            .HasIndex(m => m.Email)
            .IsUnique()
            .HasDatabaseName("IX_Members_Email");

        // API Key indexes
        modelBuilder.Entity<ApiKey>()
            .HasIndex(k => k.KeyHash)
            .IsUnique()
            .HasDatabaseName("IX_ApiKeys_KeyHash");

        modelBuilder.Entity<ApiKey>()
            .HasIndex(k => new { k.IsActive, k.ExpiresAt })
            .HasDatabaseName("IX_ApiKeys_Active_Expires");
    }
}