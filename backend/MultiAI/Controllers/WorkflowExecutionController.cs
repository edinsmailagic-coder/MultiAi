using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Models.Request;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/workflows")]
    [ApiController]
    public class WorkflowExecutionController : ControllerBase
    {
        private readonly IWorkflowExecutionService executionService;

        public WorkflowExecutionController(IWorkflowExecutionService executionService)
        {
            this.executionService = executionService;
        }

        [HttpPost("{workflowId}/execute")]
        public async Task<IActionResult> Execute(int workflowId,[FromBody] ExecuteWorkflowRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await executionService.ExecuteAsync(
                workflowId,
                userId,
                request.Input);

            return Ok(result);
        }
    }
}
