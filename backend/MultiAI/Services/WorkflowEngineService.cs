using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class WorkflowEngineService : IWorkflowEngine
    {
        private readonly IEnumerable<INodeExecutorService> nodeExecutors;
        private const int MaxSteps = 200;

        public WorkflowEngineService(IEnumerable<INodeExecutorService> nodeExecutors)
        {
            this.nodeExecutors = nodeExecutors;
        }

        public async Task ExecutionAsync(
            WorkflowExecutionContext executionContext,
            List<WorkflowNode> nodes,
            List<WorkflowConnection> connections)
        {
            if (nodes.Count == 0)
                return;

            var nodeMap = nodes.ToDictionary(n => n.Id);
            var completed = new HashSet<int>();
            var activeParallel = new HashSet<int>();
            var steps = 0;

            var start = FindStartNodes(nodes, connections).FirstOrDefault();
            if (start == null)
                return;

            var queue = new Queue<WorkflowNode>();
            queue.Enqueue(start);

            while (queue.Count > 0 && steps < MaxSteps)
            {
                steps++;
                var current = queue.Dequeue();

                if (completed.Contains(current.Id) &&
                    !string.Equals(current.NodeType, "Loop", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!IsReady(current, connections, completed) &&
                    !string.Equals(current.NodeType, "Loop", StringComparison.OrdinalIgnoreCase))
                {
                    queue.Enqueue(current);
                    continue;
                }

                if (string.Equals(current.NodeType, "Parallel", StringComparison.OrdinalIgnoreCase))
                {
                    var forkResult = await GetExecutor(current.NodeType).ExecuteAsync(executionContext, current);
                    StoreResult(executionContext, current.Id, forkResult);
                    completed.Add(current.Id);

                    var outs = Outgoing(connections, current.Id)
                        .Where(c => string.IsNullOrWhiteSpace(c.ConditionKey))
                        .ToList();

                    var tasks = new List<Task>();
                    var branchNodes = new List<WorkflowNode>();
                    foreach (var edge in outs)
                    {
                        if (!nodeMap.TryGetValue(edge.TargetNodeId, out var target))
                            continue;
                        branchNodes.Add(target);
                        activeParallel.Add(target.Id);
                    }

                    foreach (var branch in branchNodes)
                    {
                        tasks.Add(ExecuteUntilJoinAsync(executionContext, branch, nodes, connections, nodeMap, completed, activeParallel));
                    }

                    if (tasks.Count > 0)
                        await Task.WhenAll(tasks);

                    foreach (var join in nodes.Where(n =>
                                 string.Equals(n.NodeType, "Join", StringComparison.OrdinalIgnoreCase) &&
                                 !completed.Contains(n.Id) &&
                                 IsReady(n, connections, completed)))
                    {
                        queue.Enqueue(join);
                    }

                    continue;
                }

                var executor = GetExecutor(current.NodeType);
                var result = await executor.ExecuteAsync(executionContext, current);
                StoreResult(executionContext, current.Id, result);

                if (!string.Equals(current.NodeType, "Loop", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(result.Output, "done", StringComparison.OrdinalIgnoreCase))
                {
                    completed.Add(current.Id);
                }

                foreach (var next in ResolveNextNodes(current, result, connections, nodeMap))
                {
                    if (string.Equals(current.NodeType, "Loop", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(result.Output, "body", StringComparison.OrdinalIgnoreCase))
                    {
                        completed.Remove(next.Id);
                    }

                    if (!queue.Contains(next))
                        queue.Enqueue(next);
                }
            }
        }

        private async Task ExecuteUntilJoinAsync(
            WorkflowExecutionContext executionContext,
            WorkflowNode start,
            List<WorkflowNode> nodes,
            List<WorkflowConnection> connections,
            Dictionary<int, WorkflowNode> nodeMap,
            HashSet<int> completed,
            HashSet<int> activeParallel)
        {
            var localQueue = new Queue<WorkflowNode>();
            localQueue.Enqueue(start);
            var guard = 0;

            while (localQueue.Count > 0 && guard++ < MaxSteps)
            {
                var current = localQueue.Dequeue();
                if (string.Equals(current.NodeType, "Join", StringComparison.OrdinalIgnoreCase))
                    return;

                if (completed.Contains(current.Id))
                    continue;

                var executor = GetExecutor(current.NodeType);
                var result = await executor.ExecuteAsync(executionContext, current);
                StoreResult(executionContext, current.Id, result);
                completed.Add(current.Id);
                activeParallel.Remove(current.Id);

                foreach (var next in ResolveNextNodes(current, result, connections, nodeMap))
                {
                    if (string.Equals(next.NodeType, "Join", StringComparison.OrdinalIgnoreCase))
                        continue;
                    localQueue.Enqueue(next);
                }
            }
        }

        private static IEnumerable<WorkflowNode> ResolveNextNodes(
            WorkflowNode current,
            NodeExecutionResult result,
            List<WorkflowConnection> connections,
            Dictionary<int, WorkflowNode> nodeMap)
        {
            var outs = Outgoing(connections, current.Id).ToList();
            if (outs.Count == 0)
                yield break;

            if (string.Equals(current.NodeType, "If", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(current.NodeType, "Loop", StringComparison.OrdinalIgnoreCase))
            {
                var branch = result.Outputs.TryGetValue("branch", out var b)
                    ? b?.ToString()
                    : result.Output;

                var match = outs
                    .Where(c => string.Equals(c.ConditionKey, branch, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.Priority)
                    .ToList();

                if (match.Count == 0)
                {
                    match = outs
                        .Where(c => string.Equals(c.ConditionKey, "default", StringComparison.OrdinalIgnoreCase) ||
                                    string.IsNullOrWhiteSpace(c.ConditionKey))
                        .OrderBy(c => c.Priority)
                        .Take(1)
                        .ToList();
                }

                foreach (var edge in match)
                {
                    if (nodeMap.TryGetValue(edge.TargetNodeId, out var next))
                        yield return next;
                }

                yield break;
            }

            foreach (var edge in outs.OrderBy(c => c.Priority))
            {
                if (nodeMap.TryGetValue(edge.TargetNodeId, out var next))
                    yield return next;
            }
        }

        private static bool IsReady(WorkflowNode node, List<WorkflowConnection> connections, HashSet<int> completed)
        {
            var incoming = connections.Where(c => c.TargetNodeId == node.Id).ToList();
            if (incoming.Count == 0)
                return true;

            // Loop body back-edge: allow re-entry from incomplete loop controller.
            if (incoming.Any(c => string.Equals(c.ConditionKey, "body", StringComparison.OrdinalIgnoreCase)))
            {
                var nonBody = incoming.Where(c => !string.Equals(c.ConditionKey, "body", StringComparison.OrdinalIgnoreCase)).ToList();
                return nonBody.Count == 0 || nonBody.All(c => completed.Contains(c.SourceNodeId));
            }

            return incoming.All(c => completed.Contains(c.SourceNodeId));
        }

        private static List<WorkflowNode> FindStartNodes(List<WorkflowNode> nodes, List<WorkflowConnection> connections)
        {
            return nodes
                .Where(node => !connections.Any(c =>
                    c.TargetNodeId == node.Id &&
                    !string.Equals(c.ConditionKey, "body", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(n => n.Order)
                .ThenBy(n => n.Id)
                .ToList();
        }

        private static IEnumerable<WorkflowConnection> Outgoing(List<WorkflowConnection> connections, int sourceId) =>
            connections.Where(c => c.SourceNodeId == sourceId).OrderBy(c => c.Priority);

        private static void StoreResult(WorkflowExecutionContext executionContext, int nodeId, NodeExecutionResult result)
        {
            executionContext.NodeResults[nodeId] = result;
            executionContext.ExecutionLog.Add(result);
        }

        private INodeExecutorService GetExecutor(string nodeType)
        {
            var executor = nodeExecutors.FirstOrDefault(x =>
                x.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase));

            if (executor == null)
                throw new Exception($"Executor for node type '{nodeType}' not found.");

            return executor;
        }
    }
}
