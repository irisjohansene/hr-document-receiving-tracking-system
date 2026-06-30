using System.Text;
using HRDocs.Infrastructure;
using HRDocs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDocs.Api.Controllers;
[ApiController, Authorize(Roles = AppRoles.Admin), Route("api/admin")]
public sealed class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard()
    {
        var counts = await db.DocumentRequests.GroupBy(x => x.Status).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var recent = await Query(new ReportFilter()).OrderByDescending(x => x.CreatedAtUtc).Take(8).Select(x => new DocumentRequestDto { Id = x.Id, TrackingNumber = x.TrackingNumber, ControlNumber = x.ControlNumber, Title = x.Title, DocumentType = x.DocumentType.Name, SenderDepartment = x.SenderDepartment.Name, RecipientDepartment = x.RecipientDepartment.Name, SubmittedBy = x.SubmittedByUser.FullName, Status = x.Status, CreatedAtUtc = x.CreatedAtUtc, UpdatedAtUtc = x.UpdatedAtUtc }).ToListAsync();
        return Ok(new DashboardDto(Get(DocumentRequestStatus.Pending), Get(DocumentRequestStatus.Approved), Get(DocumentRequestStatus.Rejected), Get(DocumentRequestStatus.Completed), recent));
        int Get(DocumentRequestStatus s) => counts.GetValueOrDefault(s);
    }
    [HttpGet("reports")]
    public async Task<List<DocumentRequestDto>> Report([FromQuery] ReportFilter f) => await Query(f).OrderByDescending(x => x.CreatedAtUtc).Select(x => new DocumentRequestDto { Id = x.Id, TrackingNumber = x.TrackingNumber, ControlNumber = x.ControlNumber, Title = x.Title, DocumentType = x.DocumentType.Name, SenderDepartment = x.SenderDepartment.Name, RecipientDepartment = x.RecipientDepartment.Name, SubmittedBy = x.SubmittedByUser.FullName, Status = x.Status, AdminRemarks = x.AdminRemarks, CreatedAtUtc = x.CreatedAtUtc, UpdatedAtUtc = x.UpdatedAtUtc }).ToListAsync();
    [HttpGet("reports/csv")]
    public async Task<IActionResult> Csv([FromQuery] ReportFilter f)
    {
        var rows = await Report(f); var b = new StringBuilder("Tracking Number,Control Number,Title,Type,Sender Department,Recipient Department,Submitted By,Status,Created UTC\r\n");
        foreach (var x in rows) b.AppendLine(string.Join(',', new[] { x.TrackingNumber, x.ControlNumber, x.Title, x.DocumentType, x.SenderDepartment, x.RecipientDepartment, x.SubmittedBy, x.Status.ToString(), x.CreatedAtUtc.ToString("O") }.Select(CsvValue)));
        return File(Encoding.UTF8.GetBytes(b.ToString()), "text/csv", $"hrdocs-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
    private IQueryable<DocumentRequest> Query(ReportFilter f)
    {
        var q = db.DocumentRequests.AsNoTracking().Include(x => x.DocumentType).Include(x => x.SenderDepartment).Include(x => x.RecipientDepartment).Include(x => x.SubmittedByUser).AsQueryable();
        if (f.From is not null) q = q.Where(x => x.CreatedAtUtc >= f.From.Value.ToUniversalTime()); if (f.To is not null) q = q.Where(x => x.CreatedAtUtc < f.To.Value.Date.AddDays(1).ToUniversalTime()); if (f.Status is not null) q = q.Where(x => x.Status == f.Status); if (f.DepartmentId is not null) q = q.Where(x => x.SenderDepartmentId == f.DepartmentId || x.RecipientDepartmentId == f.DepartmentId); if (f.DocumentTypeId is not null) q = q.Where(x => x.DocumentTypeId == f.DocumentTypeId); return q;
    }
    private static string CsvValue(string? value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";
}
