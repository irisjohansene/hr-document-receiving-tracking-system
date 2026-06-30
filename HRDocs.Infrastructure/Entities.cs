using HRDocs.Shared;

namespace HRDocs.Infrastructure;

public abstract class Entity { public int Id { get; set; } }
public sealed class Role : Entity { public string Name { get; set; } = ""; public ICollection<User> Users { get; set; } = []; }
public sealed class Department : Entity { public string Name { get; set; } = ""; public bool IsActive { get; set; } = true; }
public sealed class DocumentType : Entity { public string Name { get; set; } = ""; public bool IsActive { get; set; } = true; }
public sealed class User : Entity
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class DocumentRequest : Entity
{
    public string TrackingNumber { get; set; } = "";
    public string? ControlNumber { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = null!;
    public int SenderDepartmentId { get; set; }
    public Department SenderDepartment { get; set; } = null!;
    public int RecipientDepartmentId { get; set; }
    public Department RecipientDepartment { get; set; } = null!;
    public int SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public DocumentRequestStatus Status { get; set; } = DocumentRequestStatus.Pending;
    public string? AdminRemarks { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<DocumentAttachment> Attachments { get; set; } = [];
    public ICollection<DocumentSignature> Signatures { get; set; } = [];
    public ICollection<DocumentStatusHistory> StatusHistory { get; set; } = [];
}
public sealed class DocumentAttachment : Entity
{
    public int DocumentRequestId { get; set; }
    public DocumentRequest DocumentRequest { get; set; } = null!;
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class DocumentSignature : Entity
{
    public int DocumentRequestId { get; set; }
    public DocumentRequest DocumentRequest { get; set; } = null!;
    public int SignerUserId { get; set; }
    public User SignerUser { get; set; } = null!;
    public string SignerRole { get; set; } = "";
    public string SignatureDataUrl { get; set; } = "";
    public DateTime SignedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class DocumentStatusHistory : Entity
{
    public int DocumentRequestId { get; set; }
    public DocumentRequest DocumentRequest { get; set; } = null!;
    public DocumentRequestStatus Status { get; set; }
    public string? Remarks { get; set; }
    public int ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class AuditLog : Entity
{
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
