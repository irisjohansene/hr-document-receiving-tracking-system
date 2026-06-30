using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HRDocs.Infrastructure;
using HRDocs.Shared;
using Microsoft.IdentityModel.Tokens;

namespace HRDocs.Api.Services;
public sealed class TokenService(IConfiguration config)
{
    public AuthResponse Create(User user)
    {
        var expires = DateTime.UtcNow.AddHours(8);
        var key = Environment.GetEnvironmentVariable("JWT_KEY") ?? config["Jwt:Key"]!;
        Claim[] claims = [new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.FullName), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Role, user.Role.Name)];
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: expires, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, new UserInfo(user.Id, user.FullName, user.Email, user.Role.Name, user.DepartmentId, user.Department?.Name));
    }
}
