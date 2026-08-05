using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services;
using MultiAI.Services.Interface;

namespace MultiAI.Tests;

public class WorkflowEngineTests
{
    [Fact]
    public async Task IfNode_FollowsTrueBranch()
    {
        var engine = new WorkflowEngineService(new INodeExecutorService[]
        {
            new IfNodeExecutorService(),
            new MarkerExecutor("A"),
            new MarkerExecutor("B"),
        });

        var nodes = new List<WorkflowNode>
        {
            new() { Id = 1, Name = "if", NodeType = "If", Prompt = "contains:yes", Order = 0 },
            new() { Id = 2, Name = "trueBranch", NodeType = "A", Order = 1 },
            new() { Id = 3, Name = "falseBranch", NodeType = "B", Order = 2 },
        };

        var connections = new List<WorkflowConnection>
        {
            new() { Id = 1, SourceNodeId = 1, TargetNodeId = 2, ConditionKey = "true" },
            new() { Id = 2, SourceNodeId = 1, TargetNodeId = 3, ConditionKey = "false" },
        };

        var context = new WorkflowExecutionContext
        {
            InitialInput = "answer is yes",
            Variables = new Dictionary<string, object> { ["lastOutput"] = "answer is yes" }
        };

        await engine.ExecutionAsync(context, nodes, connections);

        Assert.True(context.NodeResults.ContainsKey(1));
        Assert.True(context.NodeResults.ContainsKey(2));
        Assert.False(context.NodeResults.ContainsKey(3));
        Assert.Equal("true", context.NodeResults[1].Output);
    }

    [Fact]
    public async Task LoopNode_StopsAfterMaxIterations()
    {
        var engine = new WorkflowEngineService(new INodeExecutorService[]
        {
            new LoopNodeExecutorService(),
            new MarkerExecutor("Body"),
        });

        var nodes = new List<WorkflowNode>
        {
            new() { Id = 1, Name = "loop", NodeType = "Loop", Prompt = "2", Order = 0 },
            new() { Id = 2, Name = "body", NodeType = "Body", Order = 1 },
        };

        var connections = new List<WorkflowConnection>
        {
            new() { Id = 1, SourceNodeId = 1, TargetNodeId = 2, ConditionKey = "body" },
            new() { Id = 2, SourceNodeId = 2, TargetNodeId = 1, ConditionKey = "body" },
        };

        var context = new WorkflowExecutionContext { InitialInput = "start" };
        await engine.ExecutionAsync(context, nodes, connections);

        Assert.True(context.ExecutionLog.Count(x => x.NodeId == 1) >= 2);
        Assert.Contains(context.ExecutionLog, x => x.NodeId == 1 && x.Output == "done");
    }

    private sealed class MarkerExecutor(string type) : INodeExecutorService
    {
        public string NodeType => type;

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var output = $"{type}-ok";
            executionContext.Variables["lastOutput"] = output;
            return Task.FromResult(new NodeExecutionResult
            {
                NodeId = node.Id,
                Success = true,
                Output = output,
                ExecutedAt = DateTime.UtcNow
            });
        }
    }
}

public class IfNodeExecutorTests
{
    [Theory]
    [InlineData("contains:ok", "this is ok", "true")]
    [InlineData("equals:done", "done", "true")]
    [InlineData("equals:done", "nope", "false")]
    [InlineData("notEmpty", "x", "true")]
    [InlineData("notEmpty", "", "false")]
    public async Task Evaluate_Expressions(string prompt, string input, string expected)
    {
        var executor = new IfNodeExecutorService();
        var context = new WorkflowExecutionContext
        {
            InitialInput = input,
            Variables = new Dictionary<string, object> { ["lastOutput"] = input }
        };
        var node = new WorkflowNode { Id = 9, NodeType = "If", Prompt = prompt };
        var result = await executor.ExecuteAsync(context, node);
        Assert.Equal(expected, result.Output);
    }
}

public class AttachmentTextExtractorTests
{
    [Fact]
    public void Extract_PlainText()
    {
        var extractor = new AttachmentTextExtractor();
        var text = extractor.Extract("note.txt", "text/plain", System.Text.Encoding.UTF8.GetBytes("hello world"));
        Assert.Equal("hello world", text);
    }
}
