using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/workflow")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService service;

        public WorkflowController(IWorkflowService service)
        {
            this.service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var response = await service.GetAllAsync(userId);

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int workflowId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var response = await service.GetByIdAsync(workflowId, userId);

            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateWorkflow(string name, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var response = await service.CreateAsync(userId, name, description);

            return Ok(response);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkflow(int id, [FromBody] Workflow workflow)
        {
            if (id != workflow.Id)
            {
                return BadRequest("Route id and workflow id do not match.");
            }
            await service.UpdateAsync(workflow);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkflow(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await service.DeleteAsync(id,userId);

            return Ok();
        }
    }
}
