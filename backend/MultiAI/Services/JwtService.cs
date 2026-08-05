using Microsoft.IdentityModel.Tokens;
using MultiAI.Authorization;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MultiAI.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration configuration;
        private readonly IPermissionService permissionService;

        public JwtService(IConfiguration configuration, IPermissionService permissionService)
        {
            this.configuration = configuration;
            this.permissionService = permissionService;
        }

        public async Task<string> CreateTokenAsync(Users user, CancellationToken cancellationToken = default)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
            };

            foreach (var role in await permissionService.GetUserRolesAsync(user, cancellationToken))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (var permission in await permissionService.GetUserPermissionsAsync(user, cancellationToken))
            {
                claims.Add(new Claim(AppClaimTypes.Permission, permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
