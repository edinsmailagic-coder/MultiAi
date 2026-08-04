using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{workflowId}")]
        public async Task<IActionResult> GetNodes(int workflowId)
        {
            var response = await service.GetNodesAsync(workflowId);

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> AddNode([FromBody] CreateWorkflowNodeDto dto)
        {
            var response = await service.AddNodeAsync(dto);

            return Ok(response);
        }

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            await service.DeleteNodeAsync(id);

            return NoContent();
        }
    }
}
