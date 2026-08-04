using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{workflowId}")]
        public async Task<IActionResult> Get(int workflowId)
        {
            var response = await service.GetAsync(workflowId);

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Add(CreateWorkflowConnectionDto dto)
        {
            var response = await service.AddAsync(dto);

            return Ok(response);
        }

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
