using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRDocs.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db, string adminEmail, string adminPassword)
    {
        await db.Database.MigrateAsync();
        if (!await db.Users.AnyAsync(x => x.RoleId == 1))
        {
            var admin = new User { FullName = "System Administrator", Email = adminEmail.Trim().ToLowerInvariant(), RoleId = 1, DepartmentId = 1 };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, adminPassword);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
