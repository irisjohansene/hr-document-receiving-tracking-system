using HRDocs.Api.Services;
using HRDocs.Infrastructure;
using HRDocs.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRDocs.Api.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest input)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) return Conflict("An account with this email already exists.");
        if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId && x.IsActive)) return BadRequest("Invalid department.");
        var user = new User { FullName = input.FullName.Trim(), Email = email, DepartmentId = input.DepartmentId, RoleId = 2 };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, input.Password);
        db.Users.Add(user); await db.SaveChangesAsync();
        await db.Entry(user).Reference(x => x.Role).LoadAsync(); await db.Entry(user).Reference(x => x.Department).LoadAsync();
        return Ok(tokens.Create(user));
    }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest input)
    {
        var user = await db.Users.Include(x => x.Role).Include(x => x.Department).SingleOrDefaultAsync(x => x.Email == input.Email.Trim().ToLower() && x.IsActive);
        if (user is null || new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, input.Password) == PasswordVerificationResult.Failed) return Unauthorized("Invalid email or password.");
        return Ok(tokens.Create(user));
    }
}
