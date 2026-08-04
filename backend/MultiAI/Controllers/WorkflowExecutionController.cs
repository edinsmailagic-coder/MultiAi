using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Models.DTOs;
using MultiAI.Models.Request;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/workflows")]
    [ApiController]
    [Authorize]
    public class WorkflowExecutionController : ControllerBase
    {
        private readonly IWorkflowExecutionService executionService;

        public WorkflowExecutionController(IWorkflowExecutionService executionService)
        {
            this.executionService = executionService;
        }

        [HttpPost("{workflowId}/execute")]
        public async Task<IActionResult> Execute(int workflowId, [FromBody] ExecuteWorkflowRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await executionService.ExecuteAsync(
                workflowId,
                userId,
                request.Input);

            var nodeResults = result.NodeResults.Values
                .OrderBy(x => x.ExecutedAt)
                .Select(x => new NodeExecutionResultDto
                {
                    NodeId = x.NodeId,
                    Success = x.Success,
                    Output = x.Output,
                    Error = x.Error,
                    DurationMs = x.DurationMs,
                    PromptTokens = x.PromptTokens,
                    CompletionTokens = x.CompletionTokens,
                    TotalTokens = x.TotalTokens,
                    Cost = x.Cost,
                    ExecutedAt = x.ExecutedAt
                })
                .ToList();

            return Ok(new WorkflowExecutionResultDto
            {
                WorkflowId = result.WorkflowId,
                InitialInput = result.InitialInput,
                Success = nodeResults.Count > 0 && nodeResults.All(x => x.Success),
                NodeResults = nodeResults
            });
        }
    }
}
