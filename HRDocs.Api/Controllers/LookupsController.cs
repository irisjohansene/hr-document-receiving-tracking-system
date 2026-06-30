using HRDocs.Infrastructure;
using HRDocs.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDocs.Api.Controllers;
[ApiController, Route("api/lookups")]
public sealed class LookupsController(AppDbContext db) : ControllerBase
{
    [HttpGet("departments")] public Task<List<LookupItem>> Departments() => db.Departments.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
    [HttpGet("document-types")] public Task<List<LookupItem>> Types() => db.DocumentTypes.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
}
