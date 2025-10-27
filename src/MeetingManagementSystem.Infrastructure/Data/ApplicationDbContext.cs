using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MeetingManagementSystem.Core.Entities;

namespace MeetingManagementSystem.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingRoom> MeetingRooms { get; set; }
    public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
    public DbSet<AgendaItem> AgendaItems { get; set; }
    public DbSet<ActionItem> ActionItems { get; set; }
    public DbSet<MeetingDocument> MeetingDocuments { get; set; }
    public DbSet<MeetingMinutes> MeetingMinutes { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ScheduledReminder> ScheduledReminders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Meeting entity
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedMeetings)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MeetingRoom)
                .WithMany(r => r.Meetings)
                .HasForeignKey(e => e.MeetingRoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // PostgreSQL specific indexes
            entity.HasIndex(e => new { e.ScheduledDate, e.StartTime })
                .HasDatabaseName("IX_Meeting_DateTime");
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.OfficeLocation).HasMaxLength(200);

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_User_Email");
        });

        // Configure MeetingRoom entity
        modelBuilder.Entity<MeetingRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Equipment).HasMaxLength(500);
        });

        // Configure MeetingParticipant entity
        modelBuilder.Entity<MeetingParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.Participants)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.MeetingParticipations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique participant per meeting
            entity.HasIndex(e => new { e.MeetingId, e.UserId })
                .IsUnique()
                .HasDatabaseName("IX_MeetingParticipant_Unique");
        });

        // Configure AgendaItem entity
        modelBuilder.Entity<AgendaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.AgendaItems)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Presenter)
                .WithMany(u => u.PresentedAgendaItems)
                .HasForeignKey(e => e.PresenterId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.MeetingId, e.OrderIndex })
                .HasDatabaseName("IX_AgendaItem_MeetingOrder");
        });

        // Configure ActionItem entity
        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.AgendaItem)
                .WithMany(a => a.ActionItems)
                .HasForeignKey(e => e.AgendaItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedTo)
                .WithMany(u => u.AssignedActionItems)
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.DueDate)
                .HasDatabaseName("IX_ActionItem_DueDate");
            
            entity.HasIndex(e => new { e.AssignedToId, e.Status })
                .HasDatabaseName("IX_ActionItem_AssignedStatus");
        });

        // Configure MeetingDocument entity
        modelBuilder.Entity<MeetingDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.Documents)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedBy)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.MeetingId)
                .HasDatabaseName("IX_MeetingDocument_Meeting");
        });

        // Configure MeetingMinutes entity
        modelBuilder.Entity<MeetingMinutes>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasColumnType("text");
            
            entity.HasOne(e => e.Meeting)
                .WithOne(m => m.Minutes)
                .HasForeignKey<MeetingMinutes>(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedMinutes)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.MeetingId)
                .IsUnique()
                .HasDatabaseName("IX_MeetingMinutes_Meeting");
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Changes).HasColumnType("jsonb"); // PostgreSQL JSONB for structured data
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_AuditLog_Timestamp");
            
            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_AuditLog_Entity");
            
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditLog_User");
        });

        // Configure ScheduledReminder entity
        modelBuilder.Entity<ScheduledReminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            
            entity.HasOne(e => e.Meeting)
                .WithMany()
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ActionItem)
                .WithMany()
                .HasForeignKey(e => e.ActionItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ScheduledTime, e.IsSent })
                .HasDatabaseName("IX_ScheduledReminder_Processing");
            
            entity.HasIndex(e => e.MeetingId)
                .HasDatabaseName("IX_ScheduledReminder_Meeting");
        });
    }
}