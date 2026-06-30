using System.ComponentModel.DataAnnotations;

namespace HRDocs.Shared;

public enum DocumentRequestStatus { Pending, Approved, Rejected, Completed }
public static class AppRoles { public const string Admin = "Admin"; public const string User = "User"; }

public sealed class LoginRequest { [Required, EmailAddress] public string Email { get; set; } = ""; [Required] public string Password { get; set; } = ""; }
public sealed class RegisterRequest
{
    [Required, StringLength(120)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public int DepartmentId { get; set; }
}
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserInfo User);
public sealed record UserInfo(int Id, string FullName, string Email, string Role, int? DepartmentId, string? DepartmentName);
public sealed record LookupItem(int Id, string Name);

public sealed class CreateDocumentRequest
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required] public int DocumentTypeId { get; set; }
    [StringLength(2000)] public string Description { get; set; } = "";
    [Required] public int SenderDepartmentId { get; set; }
    [Required] public int RecipientDepartmentId { get; set; }
    public string? ClientSignatureDataUrl { get; set; }
}

public sealed class ReviewDocumentRequest
{
    [Required] public DocumentRequestStatus Status { get; set; }
    [StringLength(60)] public string? ControlNumber { get; set; }
    [StringLength(2000)] public string? AdminRemarks { get; set; }
    public string? AdminSignatureDataUrl { get; set; }
}

public sealed record AttachmentDto(int Id, string OriginalFileName, string ContentType, long FileSize);
public sealed record SignatureDto(string SignerName, string SignerRole, string SignatureDataUrl, DateTime SignedAtUtc);
public sealed record StatusHistoryDto(DocumentRequestStatus Status, string? Remarks, string ChangedBy, DateTime ChangedAtUtc);
public sealed class DocumentRequestDto
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string? ControlNumber { get; set; }
    public string Title { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string Description { get; set; } = "";
    public string SenderDepartment { get; set; } = "";
    public string RecipientDepartment { get; set; } = "";
    public string SubmittedBy { get; set; } = "";
    public DocumentRequestStatus Status { get; set; }
    public string? AdminRemarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = [];
    public List<SignatureDto> Signatures { get; set; } = [];
    public List<StatusHistoryDto> StatusHistory { get; set; } = [];
}

public sealed record DashboardDto(int Pending, int Approved, int Rejected, int Completed, IReadOnlyList<DocumentRequestDto> Recent);
public sealed class ReportFilter
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public DocumentRequestStatus? Status { get; set; }
    public int? DepartmentId { get; set; }
    public int? DocumentTypeId { get; set; }
}
public sealed record NotificationDto(string Message, int RequestId, DocumentRequestStatus Status, DateTime OccurredAtUtc);
