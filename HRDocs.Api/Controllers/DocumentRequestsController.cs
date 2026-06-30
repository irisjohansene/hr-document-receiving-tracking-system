using System.Security.Claims;
using System.Text.Json;
using HRDocs.Api.Hubs;
using HRDocs.Api.Services;
using HRDocs.Infrastructure;
using HRDocs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HRDocs.Api.Controllers;
[ApiController, Authorize, Route("api/requests")]
public sealed class DocumentRequestsController(AppDbContext db, IWebHostEnvironment env, IHubContext<NotificationHub> hub, PdfService pdf) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole(AppRoles.Admin);

    [HttpGet]
    public async Task<List<DocumentRequestDto>> List([FromQuery] DocumentRequestStatus? status = null)
    {
        var q = BaseQuery();
        if (!IsAdmin) q = q.Where(x => x.SubmittedByUserId == UserId);
        if (status is not null) q = q.Where(x => x.Status == status);
        return await q.OrderByDescending(x => x.CreatedAtUtc).Select(Map()).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentRequestDto>> Get(int id)
    {
        var q = BaseQuery().Where(x => x.Id == id);
        if (!IsAdmin) q = q.Where(x => x.SubmittedByUserId == UserId);
        var item = await q.Select(Map()).SingleOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, RequestSizeLimit(15_000_000)]
    public async Task<ActionResult<DocumentRequestDto>> Create([FromForm] string data, [FromForm] IFormFile? attachment)
    {
        var input = JsonSerializer.Deserialize<CreateDocumentRequest>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (input is null || string.IsNullOrWhiteSpace(input.Title)) return BadRequest("Request information is invalid.");
        if (attachment is { Length: > 10_000_000 }) return BadRequest("Attachment must not exceed 10 MB.");
        var item = new DocumentRequest { TrackingNumber = $"HRD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}", Title = input.Title.Trim(), Description = input.Description.Trim(), DocumentTypeId = input.DocumentTypeId, SenderDepartmentId = input.SenderDepartmentId, RecipientDepartmentId = input.RecipientDepartmentId, SubmittedByUserId = UserId };
        item.StatusHistory.Add(new DocumentStatusHistory { Status = DocumentRequestStatus.Pending, Remarks = "Request submitted", ChangedByUserId = UserId });
        if (!string.IsNullOrWhiteSpace(input.ClientSignatureDataUrl)) item.Signatures.Add(new DocumentSignature { SignerUserId = UserId, SignerRole = AppRoles.User, SignatureDataUrl = input.ClientSignatureDataUrl });
        if (attachment is not null)
        {
            var allowed = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" }; var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("Allowed attachments: PDF, Word, JPG, and PNG.");
            var dir = Path.Combine(env.ContentRootPath, "uploads"); Directory.CreateDirectory(dir); var stored = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(dir, stored)); await attachment.CopyToAsync(stream);
            item.Attachments.Add(new DocumentAttachment { OriginalFileName = Path.GetFileName(attachment.FileName), StoredFileName = stored, ContentType = attachment.ContentType, FileSize = attachment.Length });
        }
        db.DocumentRequests.Add(item); db.AuditLogs.Add(Audit("Submit", "DocumentRequest", null, item.Title)); await db.SaveChangesAsync();
        await hub.Clients.Group("admins").SendAsync("Notification", new NotificationDto($"New request {item.TrackingNumber} was submitted.", item.Id, item.Status, DateTime.UtcNow));
        return CreatedAtAction(nameof(Get), new { id = item.Id }, await BaseQuery().Where(x => x.Id == item.Id).Select(Map()).SingleAsync());
    }

    [Authorize(Roles = AppRoles.Admin), HttpPut("{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewDocumentRequest input)
    {
        if (input.Status is not (DocumentRequestStatus.Approved or DocumentRequestStatus.Rejected or DocumentRequestStatus.Completed)) return BadRequest("Invalid review status.");
        var item = await db.DocumentRequests.Include(x => x.Signatures).SingleOrDefaultAsync(x => x.Id == id); if (item is null) return NotFound();
        if (input.Status is DocumentRequestStatus.Approved or DocumentRequestStatus.Completed && string.IsNullOrWhiteSpace(input.ControlNumber)) return BadRequest("A control number is required for approval or completion.");
        item.Status = input.Status; item.ControlNumber = input.ControlNumber?.Trim(); item.AdminRemarks = input.AdminRemarks?.Trim(); item.UpdatedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(input.AdminSignatureDataUrl)) item.Signatures.Add(new DocumentSignature { SignerUserId = UserId, SignerRole = AppRoles.Admin, SignatureDataUrl = input.AdminSignatureDataUrl });
        db.DocumentStatusHistories.Add(new DocumentStatusHistory { DocumentRequestId = id, Status = input.Status, Remarks = input.AdminRemarks, ChangedByUserId = UserId });
        db.AuditLogs.Add(Audit("Review", "DocumentRequest", id.ToString(), JsonSerializer.Serialize(new { input.Status, input.ControlNumber }))); await db.SaveChangesAsync();
        await hub.Clients.Group($"user-{item.SubmittedByUserId}").SendAsync("Notification", new NotificationDto($"Request {item.TrackingNumber} is now {item.Status}.", item.Id, item.Status, DateTime.UtcNow));
        return NoContent();
    }

    [HttpGet("{id:int}/attachment/{attachmentId:int}")]
    public async Task<IActionResult> Attachment(int id, int attachmentId)
    {
        var a = await db.DocumentAttachments.Include(x => x.DocumentRequest).SingleOrDefaultAsync(x => x.Id == attachmentId && x.DocumentRequestId == id);
        if (a is null || (!IsAdmin && a.DocumentRequest.SubmittedByUserId != UserId)) return NotFound();
        var path = Path.Combine(env.ContentRootPath, "uploads", a.StoredFileName); return System.IO.File.Exists(path) ? PhysicalFile(path, a.ContentType, a.OriginalFileName) : NotFound();
    }

    [HttpGet("{id:int}/proof")]
    public async Task<IActionResult> Proof(int id)
    {
        var item = await BaseQuery().SingleOrDefaultAsync(x => x.Id == id); if (item is null || (!IsAdmin && item.SubmittedByUserId != UserId)) return NotFound();
        if (item.Status is not (DocumentRequestStatus.Approved or DocumentRequestStatus.Completed)) return BadRequest("Receiving proof is available after approval.");
        return File(pdf.GenerateAcknowledgment(item), "application/pdf", $"Receiving-{item.TrackingNumber}.pdf");
    }

    private IQueryable<DocumentRequest> BaseQuery() => db.DocumentRequests.AsNoTracking().Include(x => x.DocumentType).Include(x => x.SenderDepartment).Include(x => x.RecipientDepartment).Include(x => x.SubmittedByUser).Include(x => x.Attachments).Include(x => x.Signatures).ThenInclude(x => x.SignerUser).Include(x => x.StatusHistory).ThenInclude(x => x.ChangedByUser).AsSplitQuery();
    private static System.Linq.Expressions.Expression<Func<DocumentRequest, DocumentRequestDto>> Map() => x => new DocumentRequestDto { Id = x.Id, TrackingNumber = x.TrackingNumber, ControlNumber = x.ControlNumber, Title = x.Title, DocumentType = x.DocumentType.Name, Description = x.Description, SenderDepartment = x.SenderDepartment.Name, RecipientDepartment = x.RecipientDepartment.Name, SubmittedBy = x.SubmittedByUser.FullName, Status = x.Status, AdminRemarks = x.AdminRemarks, CreatedAtUtc = x.CreatedAtUtc, UpdatedAtUtc = x.UpdatedAtUtc, Attachments = x.Attachments.Select(a => new AttachmentDto(a.Id, a.OriginalFileName, a.ContentType, a.FileSize)).ToList(), Signatures = x.Signatures.OrderBy(s => s.SignedAtUtc).Select(s => new SignatureDto(s.SignerUser.FullName, s.SignerRole, s.SignatureDataUrl, s.SignedAtUtc)).ToList(), StatusHistory = x.StatusHistory.OrderByDescending(h => h.ChangedAtUtc).Select(h => new StatusHistoryDto(h.Status, h.Remarks, h.ChangedByUser.FullName, h.ChangedAtUtc)).ToList() };
    private AuditLog Audit(string action, string entity, string? entityId, string? details) => new() { UserId = UserId, Action = action, EntityName = entity, EntityId = entityId, Details = details, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() };
}
