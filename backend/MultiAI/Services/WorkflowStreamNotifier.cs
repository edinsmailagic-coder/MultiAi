using MultiAI.Models.Response;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class WorkflowStreamNotifier : IWorkflowStreamNotifier
    {
        private readonly SemaphoreSlim gate = new(1, 1);

        public Func<ChatStreamEventDto, CancellationToken, Task>? Emit { get; set; }

        public Task NodeStartedAsync(
            int nodeId,
            string nodeName,
            string nodeType,
            string? model,
            CancellationToken cancellationToken) =>
            WriteAsync(
                new ChatStreamEventDto
                {
                    Type = "node-progress",
                    NodeId = nodeId,
                    NodeName = nodeName,
                    NodeType = nodeType,
                    Model = model,
                    Status = "started",
                    Content = string.IsNullOrWhiteSpace(model) ? nodeName : $"{nodeName} · {model}"
                },
                cancellationToken);

        public Task TokenDeltaAsync(string delta, CancellationToken cancellationToken) =>
            WriteAsync(
                new ChatStreamEventDto
                {
                    Type = "delta",
                    Content = delta
                },
                cancellationToken);

        private async Task WriteAsync(ChatStreamEventDto evt, CancellationToken cancellationToken)
        {
            var emit = Emit;
            if (emit == null)
                return;

            await gate.WaitAsync(cancellationToken);
            try
            {
                await emit(evt, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
