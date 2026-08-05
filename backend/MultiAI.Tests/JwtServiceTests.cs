using Microsoft.Extensions.Configuration;
using Moq;
using MultiAI.Authorization;
using MultiAI.Models.Entities;
using MultiAI.Services;
using MultiAI.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MultiAI.Tests
{
    public class JwtServiceTests
    {
        [Fact]
        public async Task CreateTokenAsync_IncludesRolesAndPermissions()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
                    ["Jwt:Issuer"] = "MultiAI.API",
                    ["Jwt:Audience"] = "MultiAI.Client",
                })
                .Build();

            var permissionService = new Mock<IPermissionService>();
            permissionService
                .Setup(x => x.GetUserRolesAsync(It.IsAny<Users>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Admin"]);
            permissionService
                .Setup(x => x.GetUserPermissionsAsync(It.IsAny<Users>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([AppPermissions.WorkflowsRead, AppPermissions.ChatWrite]);

            var sut = new JwtService(config, permissionService.Object);
            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                Email = "admin@multiai.com",
                UserName = "admin@multiai.com",
                FirstName = "System",
                LastName = "Administrator",
            };

            var token = await sut.CreateTokenAsync(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
            Assert.Contains(jwt.Claims, c => c.Type == AppClaimTypes.Permission && c.Value == AppPermissions.WorkflowsRead);
            Assert.Contains(jwt.Claims, c => c.Type == AppClaimTypes.Permission && c.Value == AppPermissions.ChatWrite);
        }
    }
}
