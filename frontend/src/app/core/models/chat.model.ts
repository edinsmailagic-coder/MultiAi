export interface ConversationFolder {
  id: number;
  name: string;
  sortOrder: number;
}

export interface AttachmentItem {
  id: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  isImage: boolean;
  extractedTextPreview?: string | null;
}

export interface ConversationListItem {
  id: number;
  title: string;
  createdAt: string;
  updatedAt?: string | null;
  isPinned?: boolean;
  pinnedAt?: string | null;
  folderId?: number | null;
}

export interface ChatMessageResult {
  id: number;
  content: string;
  createdAt: string;
  attachments?: AttachmentItem[];
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
  isPinned?: boolean;
  folderId?: number | null;
  turns: ConversationTurn[];
}

export interface SendChatRequest {
  conversationId?: number | null;
  message: string;
  userProviderId?: number | null;
  model?: string | null;
  workflowId?: number | null;
  attachmentIds?: number[];
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
  attachments?: AttachmentItem[];
}

export interface ChatStreamEvent {
  type: 'ready' | 'node-progress' | 'delta' | 'done' | 'error' | string;
  conversationId?: number | null;
  title?: string | null;
  userMessage?: ChatMessageResult | null;
  nodeId?: number | null;
  nodeName?: string | null;
  nodeType?: string | null;
  model?: string | null;
  status?: string | null;
  content?: string | null;
  response?: ChatResponse | null;
  message?: string | null;
}
