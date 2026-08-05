using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.DTOs;
using MultiAI.Services.Interface;

namespace MultiAI.Controllers
{
    [Route("api/workflow-nodes")]
    [ApiController]
    public class WorkflowNodesController : ControllerBase
    {
        private readonly IWorkflowNodeService service;

        public WorkflowNodesController(IWorkflowNodeService service)
        {
            this.service = service;
        }

        [HasPermission(AppPermissions.WorkflowsRead)]
        [HttpGet("{workflowId}")]
        public async Task<IActionResult> GetNodes(int workflowId)
        {
            var response = await service.GetNodesAsync(workflowId);

            return Ok(response);
        }

        [HasPermission(AppPermissions.WorkflowsWrite)]
        [HttpPost]
        public async Task<IActionResult> AddNode([FromBody] CreateWorkflowNodeDto dto)
        {
            var response = await service.AddNodeAsync(dto);

            return Ok(response);
        }

        [HasPermission(AppPermissions.WorkflowsWrite)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNode(
            int id,
            [FromBody] UpdateWorkflowNodeDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id mismatch.");

            await service.UpdateNodeAsync(dto);

            return NoContent();
        }

        [HasPermission(AppPermissions.WorkflowsWrite)]
        [HttpPatch("{id}/move")]
        public async Task<IActionResult> MoveNode(
            int id,
            [FromBody] MoveWorkflowNodeDto dto)
        {
            if (id != dto.NodeId)
                return BadRequest("Id mismatch.");

            await service.MoveNodeAsync(dto);

            return NoContent();
        }

        [HasPermission(AppPermissions.WorkflowsWrite)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            await service.DeleteNodeAsync(id);

            return NoContent();
        }
    }
}
