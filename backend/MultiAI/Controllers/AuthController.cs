using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Models.Log;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Users> userManager;
        private readonly SignInManager<Users> signInManager;
        private readonly IJwtService jwtService;
        private readonly IUserLogService userLog;
        private readonly IPermissionService permissionService;

        public AuthController(
            UserManager<Users> userManager,
            SignInManager<Users> signInManager,
            IJwtService jwtService,
            IUserLogService userLog,
            IPermissionService permissionService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.jwtService = jwtService;
            this.userLog = userLog;
            this.permissionService = permissionService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user == null)
            {
                await userLog.LogAsync(null, UserLogActions.LoginFailed, $"Email={dto.Email}", ip);
                return Unauthorized();
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

            if (!result.Succeeded)
            {
                await userLog.LogAsync(user.Id, UserLogActions.LoginFailed, $"Email={dto.Email}", ip);
                return Unauthorized();
            }

            var token = await jwtService.CreateTokenAsync(user);
            await userLog.LogAsync(user.Id, UserLogActions.Login, $"Email={user.Email}", ip);

            return Ok(new { token });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new Users
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await permissionService.EnsureDefaultRoleAsync(user);
            var token = await jwtService.CreateTokenAsync(user);
            await userLog.LogAsync(
                user.Id,
                UserLogActions.Register,
                $"Email={user.Email}; Name={user.FirstName} {user.LastName}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { token });
        }

        [HasPermission(AppPermissions.AuthMe)]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            return Ok(await ToProfileDtoAsync(user));
        }

        [HasPermission(AppPermissions.AuthProfileWrite)]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var oldFirst = user.FirstName;
            var oldLast = user.LastName;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = user.UserName;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await userLog.LogAsync(
                user.Id,
                UserLogActions.ProfileUpdated,
                $"FirstName: '{oldFirst}' -> '{user.FirstName}'; LastName: '{oldLast}' -> '{user.LastName}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(await ToProfileDtoAsync(user));
        }

        [HasPermission(AppPermissions.AuthPasswordChange)]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            if (!result.Succeeded)
            {
                await userLog.LogAsync(user.Id, UserLogActions.PasswordChangeFailed, null, ip);
                return BadRequest(result.Errors);
            }

            await userLog.LogAsync(user.Id, UserLogActions.PasswordChanged, null, ip);
            return NoContent();
        }

        private async Task<Users?> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId)
                ? null
                : await userManager.FindByIdAsync(userId);
        }

        private async Task<UserProfileDto> ToProfileDtoAsync(Users user)
        {
            var roles = await permissionService.GetUserRolesAsync(user);
            var permissions = await permissionService.GetUserPermissionsAsync(user);

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
            };
        }
    }
}
