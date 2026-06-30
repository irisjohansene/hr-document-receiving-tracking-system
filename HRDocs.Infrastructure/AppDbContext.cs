using HRDocs.Shared;
using Microsoft.EntityFrameworkCore;

namespace HRDocs.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();
    public DbSet<DocumentAttachment> DocumentAttachments => Set<DocumentAttachment>();
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();
    public DbSet<DocumentStatusHistory> DocumentStatusHistories => Set<DocumentStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Department>().HasIndex(x => x.Name).IsUnique();
        b.Entity<DocumentType>().HasIndex(x => x.Name).IsUnique();
        b.Entity<DocumentRequest>().HasIndex(x => x.TrackingNumber).IsUnique();
        b.Entity<DocumentRequest>().HasIndex(x => x.ControlNumber).IsUnique();
        b.Entity<DocumentRequest>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Entity<DocumentStatusHistory>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Entity<DocumentRequest>().HasOne(x => x.SenderDepartment).WithMany().HasForeignKey(x => x.SenderDepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DocumentRequest>().HasOne(x => x.RecipientDepartment).WithMany().HasForeignKey(x => x.RecipientDepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DocumentStatusHistory>().HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DocumentSignature>().HasOne(x => x.SignerUser).WithMany().HasForeignKey(x => x.SignerUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Role>().HasData(new Role { Id = 1, Name = AppRoles.Admin }, new Role { Id = 2, Name = AppRoles.User });
        b.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Human Resources" }, new Department { Id = 2, Name = "Finance" },
            new Department { Id = 3, Name = "Information Technology" }, new Department { Id = 4, Name = "Operations" });
        b.Entity<DocumentType>().HasData(
            new DocumentType { Id = 1, Name = "Memorandum" }, new DocumentType { Id = 2, Name = "Personnel Record" },
            new DocumentType { Id = 3, Name = "Application" }, new DocumentType { Id = 4, Name = "Other" });
    }
}
