using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.DTOs;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/workflow-connection")]
    [ApiController]
    public class WorkflowConnectionController : ControllerBase
    {
        private readonly IWorkflowConnectionService service;

        public WorkflowConnectionController(IWorkflowConnectionService service)
        {
            this.service = service;
        }

        [HasPermission(AppPermissions.WorkflowsRead)]
        [HttpGet("{workflowId}")]
        public async Task<IActionResult> Get(int workflowId)
        {
            var response = await service.GetAsync(workflowId);

            return Ok(response);
        }

        [HasPermission(AppPermissions.WorkflowsWrite)]
        [HttpPost]
        public async Task<IActionResult> Add(CreateWorkflowConnectionDto dto)
        {
            var response = await service.AddAsync(dto);

            return Ok(response);
        }

        [HasPermission(AppPermissions.WorkflowsWrite)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            await service.DeleteAsync(id,userId);

            return NoContent();
        }
    }
}
