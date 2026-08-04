using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class WorkflowEngineService : IWorkflowEngine
    {
        private readonly IEnumerable<INodeExecutorService> nodeExecutors;

        public WorkflowEngineService(IEnumerable<INodeExecutorService> nodeExecutors)
        {
            this.nodeExecutors = nodeExecutors;
        }
        public async Task ExecutionAsync(WorkflowExecutionContext executionContext, List<WorkflowNode> nodes,List<WorkflowConnection> connections)
        {
            if (!nodes.Any())
            {
                return;
            }
            var currentNode = FindStartNode(nodes, connections);

            while (currentNode != null)
            {
                var executor = GetExecutor(currentNode.NodeType);

                var result = await executor.ExecuteAsync(executionContext, currentNode);

                executionContext.NodeResults[currentNode.Id] = result;

                currentNode=FindNextNode(currentNode.Id,nodes, connections);
            }
        }

        private WorkflowNode? FindNextNode(int id, List<WorkflowNode> nodes, List<WorkflowConnection> connections)
        {
            var connection = connections.FirstOrDefault(x =>
                x.SourceNodeId == id);

            if (connection == null)
                return null;

            return nodes.FirstOrDefault(x =>
                x.Id == connection.TargetNodeId);
        }

        private INodeExecutorService GetExecutor(string nodeType)
        {
            var executor = nodeExecutors.FirstOrDefault(x =>
                x.NodeType.Equals(nodeType,
                    StringComparison.OrdinalIgnoreCase));

            if (executor == null)
                throw new Exception(
                    $"Executor for node type '{nodeType}' not found.");

            return executor;
        }

        private WorkflowNode? FindStartNode(List<WorkflowNode> nodes, List<WorkflowConnection> connections)
        {
            return nodes.FirstOrDefault(node =>
                !connections.Any(c => c.TargetNodeId == node.Id));
        }
    }
}
