using Microsoft.IdentityModel.Tokens;
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

        public JwtService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public string CreateToken(Users users)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,users.Id),
                new Claim(ClaimTypes.NameIdentifier, users.Id),
                new Claim(JwtRegisteredClaimNames.Email,users.Email!),
                new Claim(ClaimTypes.Name,users.UserName!)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

            var creds=new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            var token= new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience:configuration["Jwt:Audience"],
                claims:claims,
                expires:DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
