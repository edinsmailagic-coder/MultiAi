using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.DTOs;
using MultiAI.Models.Log;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService users;
        private readonly IUserLogService userLog;

        public AdminUsersController(IAdminUserService users, IUserLogService userLog)
        {
            this.users = users;
            this.userLog = userLog;
        }

        [HasPermission(AppPermissions.UsersRead)]
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            return Ok(await users.GetUsersAsync(cancellationToken));
        }

        [HasPermission(AppPermissions.UsersRead)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var user = await users.GetUserAsync(id, cancellationToken);
            return user == null ? NotFound() : Ok(user);
        }

        [HasPermission(AppPermissions.UsersManage)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdminUserDto dto, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                var created = await users.CreateUserAsync(dto, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminUserCreated,
                    $"Target={created.Id}; Email={created.Email}; Roles={string.Join(',', created.Roles)}",
                    Ip());
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HasPermission(AppPermissions.UsersManage)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateAdminUserDto dto, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                var updated = await users.UpdateUserAsync(id, dto, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminUserUpdated,
                    $"Target={updated.Id}; Email={updated.Email}; Roles={string.Join(',', updated.Roles)}; Active={!updated.IsDeleted}",
                    Ip());
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HasPermission(AppPermissions.UsersManage)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(string id, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                await users.SoftDeleteUserAsync(id, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminUserDeleted,
                    $"Target={id}",
                    Ip());
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HasPermission(AppPermissions.UsersManage)]
        [HttpPost("{id}/password")]
        public async Task<IActionResult> ResetPassword(
            string id,
            [FromBody] ResetAdminPasswordDto dto,
            CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                await users.ResetPasswordAsync(id, dto.NewPassword, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminPasswordReset,
                    $"Target={id}",
                    Ip());
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string? GetActorId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
