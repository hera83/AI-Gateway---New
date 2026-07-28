using AiGateway.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiGateway.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<ApiKeyAuditLog> ApiKeyAuditLogs => Set<ApiKeyAuditLog>();

    public DbSet<KnowledgeGroup> KnowledgeGroups => Set<KnowledgeGroup>();

    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(key => key.KeyHash).IsUnique();
            entity.Property(key => key.Name).HasMaxLength(200);
            entity.Property(key => key.ResponsibleName).HasMaxLength(200);
            entity.Property(key => key.ContactInfo).HasMaxLength(300);
        });

        modelBuilder.Entity<ApiKeyAuditLog>(entity =>
        {
            entity.Property(log => log.Action).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(log => log.ApiKeyId);
            entity
                .HasOne<ApiKey>()
                .WithMany()
                .HasForeignKey(log => log.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeGroup>(entity =>
        {
            entity.Property(group => group.Name).HasMaxLength(200);
            entity.HasIndex(group => new { group.ApiKeyId, group.Name }).IsUnique();
            entity
                .HasOne<ApiKey>()
                .WithMany()
                .HasForeignKey(group => group.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.Property(document => document.FileName).HasMaxLength(300);
            entity.Property(document => document.ContentType).HasMaxLength(200);
            entity.Property(document => document.ErrorMessage).HasMaxLength(2000);
            entity.Property(document => document.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(document => document.ApiKeyId);
            entity.HasIndex(document => document.KnowledgeGroupId);
            entity
                .HasOne<KnowledgeGroup>()
                .WithMany()
                .HasForeignKey(document => document.KnowledgeGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeChunk>(entity =>
        {
            entity.HasIndex(chunk => chunk.ApiKeyId);
            entity.HasIndex(chunk => chunk.DocumentId);
            entity.HasIndex(chunk => chunk.VectorRowId).IsUnique();
            entity
                .HasOne<KnowledgeDocument>()
                .WithMany()
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
