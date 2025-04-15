using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
    public DbSet<TodoCategory> TodoCategories { get; set; } = null!;
    public DbSet<SelfDestructEmail> SelfDestructEmails { get; set; } = null!;
    public DbSet<RecurrenceSchedule> RecurrenceSchedules { get; set; } = null!;
    public DbSet<BackupLog> BackupLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure TodoItem
        builder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Relationships
            entity.HasOne(e => e.Category)
                .WithMany(c => c.TodoItems)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.RecurrenceSchedule)
                .WithMany()
                .HasForeignKey(e => e.RecurrenceScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.TodoItems)
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure JournalEntry
        builder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Relationship with user
            entity.HasOne(e => e.User)
                .WithMany(u => u.JournalEntries)
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TodoCategory
        builder.Entity<TodoCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7); // Hex color code
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.TodoCategories)
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SelfDestructEmail
        builder.Entity<SelfDestructEmail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.AccessGuid).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.SelfDestructEmails)
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RecurrenceSchedule
        builder.Entity<RecurrenceSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Interval).IsRequired();
            entity.Property(e => e.DaysOfWeek).HasMaxLength(50); // Stored as comma-separated values
            entity.Property(e => e.MonthlyDay).HasMaxLength(10);
            entity.Property(e => e.EndDate);
        });

        // Configure BackupLog
        builder.Entity<BackupLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.FileSize);
        });
    }
} 