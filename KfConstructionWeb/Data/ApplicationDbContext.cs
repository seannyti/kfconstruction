using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KfConstructionWeb.Models;

namespace KfConstructionWeb.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<AccountLock> AccountLocks { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Testimonial> Testimonials { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<ReceiptAccessLog> ReceiptAccessLogs { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<BroadcastMessage> BroadcastMessages { get; set; }
    public DbSet<UserMessageStatus> UserMessageStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Client relationships
        modelBuilder.Entity<Client>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Testimonial relationships
        modelBuilder.Entity<Testimonial>()
            .HasOne(t => t.Client)
            .WithMany()
            .HasForeignKey(t => t.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure indexes for better performance
        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("IX_Clients_Email");

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.AccessCode)
            .IsUnique()
            .HasDatabaseName("IX_Clients_AccessCode");

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Clients_UserId");

        // Configure Testimonial indexes
        modelBuilder.Entity<Testimonial>()
            .HasIndex(t => new { t.Status, t.IsFeatured, t.DisplayOrder })
            .HasDatabaseName("IX_Testimonials_Status_Featured_Order");

        modelBuilder.Entity<Testimonial>()
            .HasIndex(t => t.ClientId)
            .HasDatabaseName("IX_Testimonials_ClientId");

        modelBuilder.Entity<Testimonial>()
            .HasIndex(t => t.ServiceType)
            .HasDatabaseName("IX_Testimonials_ServiceType");

        modelBuilder.Entity<Testimonial>()
            .HasIndex(t => t.SubmittedAt)
            .HasDatabaseName("IX_Testimonials_SubmittedAt");

        // Configure EmailLog indexes
        modelBuilder.Entity<EmailLog>()
            .HasIndex(e => new { e.ToEmail, e.CreatedAt })
            .HasDatabaseName("IX_EmailLogs_ToEmail_CreatedAt");

        modelBuilder.Entity<EmailLog>()
            .HasIndex(e => new { e.Status, e.EmailType })
            .HasDatabaseName("IX_EmailLogs_Status_Type");

        modelBuilder.Entity<EmailLog>()
            .HasIndex(e => e.RelatedEntityId)
            .HasDatabaseName("IX_EmailLogs_RelatedEntityId");

        // Configure Receipt relationships
        modelBuilder.Entity<ReceiptAccessLog>()
            .HasOne(ral => ral.Receipt)
            .WithMany(r => r.AccessLogs)
            .HasForeignKey(ral => ral.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Receipt indexes for performance (p95 < 200ms target)
        modelBuilder.Entity<Receipt>()
            .HasIndex(r => new { r.PurchaseDate, r.IsDeleted })
            .HasDatabaseName("IX_Receipts_PurchaseDate_IsDeleted");

        modelBuilder.Entity<Receipt>()
            .HasIndex(r => new { r.Vendor, r.IsDeleted })
            .HasDatabaseName("IX_Receipts_Vendor_IsDeleted");

        modelBuilder.Entity<Receipt>()
            .HasIndex(r => new { r.ReceiptNumber, r.IsDeleted })
            .HasDatabaseName("IX_Receipts_ReceiptNumber_IsDeleted");

        modelBuilder.Entity<Receipt>()
            .HasIndex(r => new { r.Category, r.IsDeleted })
            .HasDatabaseName("IX_Receipts_Category_IsDeleted");

        modelBuilder.Entity<Receipt>()
            .HasIndex(r => r.ScheduledPurgeDate)
            .HasDatabaseName("IX_Receipts_ScheduledPurgeDate");

        // Configure ReceiptAccessLog indexes for audit queries
        modelBuilder.Entity<ReceiptAccessLog>()
            .HasIndex(ral => new { ral.ReceiptId, ral.AccessedAt })
            .HasDatabaseName("IX_ReceiptAccessLogs_ReceiptId_AccessedAt");

        modelBuilder.Entity<ReceiptAccessLog>()
            .HasIndex(ral => new { ral.AccessedBy, ral.AccessedAt })
            .HasDatabaseName("IX_ReceiptAccessLogs_AccessedBy_AccessedAt");

        // Configure BroadcastMessage relationships
        modelBuilder.Entity<UserMessageStatus>()
            .HasOne(ums => ums.BroadcastMessage)
            .WithMany(bm => bm.UserStatuses)
            .HasForeignKey(ums => ums.BroadcastMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure BroadcastMessage indexes
        modelBuilder.Entity<BroadcastMessage>()
            .HasIndex(bm => new { bm.IsActive, bm.SentAt })
            .HasDatabaseName("IX_BroadcastMessages_Active_SentAt");

        modelBuilder.Entity<BroadcastMessage>()
            .HasIndex(bm => bm.TargetRole)
            .HasDatabaseName("IX_BroadcastMessages_TargetRole");

        modelBuilder.Entity<UserMessageStatus>()
            .HasIndex(ums => new { ums.UserId, ums.IsRead })
            .HasDatabaseName("IX_UserMessageStatuses_UserId_IsRead");

        modelBuilder.Entity<UserMessageStatus>()
            .HasIndex(ums => new { ums.BroadcastMessageId, ums.IsRead })
            .HasDatabaseName("IX_UserMessageStatuses_MessageId_IsRead");
    }
}
