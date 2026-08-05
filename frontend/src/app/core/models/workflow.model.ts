export interface Workflow { id: number; userId: string; name: string; description: string; isActive: boolean; createdAt?: string; }
export type WorkflowNodeType = 'AI' | 'If' | 'Parallel' | 'Join' | 'Loop' | string;
export interface WorkflowNode {
  id: number;
  workflowId: number;
  userAIProviderId: number;
  model: string;
  name: string;
  prompt: string;
  inputMapping?: string | null;
  order: number;
  positionX: number;
  positionY: number;
  nodeType: WorkflowNodeType;
}
export interface CreateWorkflowNodeDto {
  workflowId: number;
  userAIProviderId: number;
  model: string;
  name: string;
  prompt: string;
  inputMapping?: string;
  order: number;
  positionX: number;
  positionY: number;
  nodeType: WorkflowNodeType;
}
export interface UpdateWorkflowNodeDto {
  id: number;
  name: string;
  model: string;
  prompt: string;
  inputMapping?: string;
  order: number;
  nodeType: string;
}
export interface MoveWorkflowNodeDto { nodeId: number; positionX: number; positionY: number; }
export interface WorkflowConnection {
  id: number;
  workflowId: number;
  sourceNodeId: number;
  targetNodeId: number;
  sourceOutput?: string;
  targetInput?: string;
  conditionKey?: string | null;
  priority?: number;
}
export interface CreateWorkflowConnectionDto {
  workflowId: number;
  sourceNodeId: number;
  targetNodeId: number;
  conditionKey?: string | null;
  priority?: number;
  sourceOutput?: string;
  targetInput?: string;
}
export interface NodeExecutionResult {
  nodeId: number;
  success: boolean;
  output?: string;
  error?: string;
  durationMs: number;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  cost?: number;
  executedAt: string;
}
export interface WorkflowExecutionResult {
  success?: boolean;
  nodeResults?: NodeExecutionResult[] | Record<string, NodeExecutionResult>;
}
