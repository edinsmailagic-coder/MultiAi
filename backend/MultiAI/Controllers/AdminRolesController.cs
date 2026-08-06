using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.DTOs;
using MultiAI.Models.Log;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [ApiController]
    [Route("api/admin/roles")]
    public class AdminRolesController : ControllerBase
    {
        private readonly IAdminRoleService roles;
        private readonly IUserLogService userLog;

        public AdminRolesController(IAdminRoleService roles, IUserLogService userLog)
        {
            this.roles = roles;
            this.userLog = userLog;
        }

        [HasPermission(AppPermissions.RolesRead)]
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            return Ok(await roles.GetRolesAsync(cancellationToken));
        }

        [HasPermission(AppPermissions.RolesRead)]
        [HttpGet("permissions")]
        public async Task<IActionResult> Permissions()
        {
            return Ok(await roles.GetAllPermissionsAsync());
        }

        [HasPermission(AppPermissions.RolesRead)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var role = await roles.GetRoleAsync(id, cancellationToken);
            return role == null ? NotFound() : Ok(role);
        }

        [HasPermission(AppPermissions.RolesManage)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                var created = await roles.CreateRoleAsync(dto, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminRoleCreated,
                    $"Role={created.Name}; Permissions={string.Join(',', created.Permissions)}",
                    Ip());
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HasPermission(AppPermissions.RolesManage)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                var updated = await roles.UpdateRoleAsync(id, dto, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminRoleUpdated,
                    $"Role={updated.Name}; Permissions={string.Join(',', updated.Permissions)}",
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

        [HasPermission(AppPermissions.RolesManage)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            if (actorId == null)
                return Unauthorized();

            try
            {
                await roles.DeleteRoleAsync(id, actorId, cancellationToken);
                await userLog.LogAsync(
                    actorId,
                    UserLogActions.AdminRoleDeleted,
                    $"RoleId={id}",
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
