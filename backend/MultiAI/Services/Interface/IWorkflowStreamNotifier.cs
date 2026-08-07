using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    /// <summary>
    /// Scoped sink used to push SSE frames (node status, deltas) during workflow execution.
    /// </summary>
    public interface IWorkflowStreamNotifier
    {
        Func<ChatStreamEventDto, CancellationToken, Task>? Emit { get; set; }

        Task NodeStartedAsync(int nodeId, string nodeName, string nodeType, string? model, CancellationToken cancellationToken);

        Task TokenDeltaAsync(string delta, CancellationToken cancellationToken);
    }
}
