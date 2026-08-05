export interface ConversationListItem {
  id: number;
  title: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface ChatMessageResult {
  id: number;
  content: string;
  createdAt: string;
}

export interface ChatResponse {
  id: number;
  model: string;
  content: string;
  promptTokens: number;
  completitionTokens: number;
  totalTokens: number;
  durationMs: number;
  userProviderId?: number | null;
  workflowNodeId?: number | null;
  createdAt: string;
}

export interface ConversationTurn {
  userMessage: ChatMessageResult;
  responses: ChatResponse[];
}

export interface ConversationDetail {
  id: number;
  title: string;
  createdAt: string;
  updatedAt?: string | null;
  turns: ConversationTurn[];
}

export interface SendChatRequest {
  conversationId?: number | null;
  message: string;
  userProviderId?: number | null;
  model?: string | null;
  workflowId?: number | null;
}

export interface SendChatResult {
  conversationId: number;
  title: string;
  userMessage: ChatMessageResult;
  response: ChatResponse;
}

export interface ChatUiMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  model?: string;
  createdAt?: string;
}
