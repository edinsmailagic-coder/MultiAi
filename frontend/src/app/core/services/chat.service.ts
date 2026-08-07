import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AttachmentItem,
  ChatStreamEvent,
  ConversationDetail,
  ConversationFolder,
  ConversationListItem,
  SendChatRequest,
  SendChatResult,
} from '../models/chat.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly api = `${environment.apiUrl}/api/chat`;
  private readonly auth = inject(AuthService);

  constructor(private readonly http: HttpClient) {}

  getConversations(): Observable<ConversationListItem[]> {
    return this.http.get<ConversationListItem[]>(`${this.api}/conversations`);
  }

  getConversation(id: number): Observable<ConversationDetail> {
    return this.http.get<ConversationDetail>(`${this.api}/conversations/${id}`);
  }

  send(request: SendChatRequest): Observable<SendChatResult> {
    return this.http.post<SendChatResult>(`${this.api}/sendChat`, request);
  }

  streamSend(request: SendChatRequest): Observable<ChatStreamEvent> {
    return new Observable<ChatStreamEvent>((subscriber) => {
      const controller = new AbortController();
      const token = this.auth.token();

      void (async () => {
        try {
          const response = await fetch(`${this.api}/stream`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              Accept: 'text/event-stream',
              ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
            body: JSON.stringify(request),
            signal: controller.signal,
          });

          if (!response.ok || !response.body) {
            let message = `HTTP ${response.status}`;
            try {
              const body = (await response.json()) as { error?: string };
              if (body?.error) {
                message = body.error;
              }
            } catch {
              // ignore parse errors
            }
            subscriber.error(new Error(message));
            return;
          }

          const reader = response.body.getReader();
          const decoder = new TextDecoder();
          let buffer = '';

          while (true) {
            const { done, value } = await reader.read();
            if (done) {
              break;
            }

            buffer += decoder.decode(value, { stream: true });
            const chunks = buffer.split('\n\n');
            buffer = chunks.pop() ?? '';

            for (const chunk of chunks) {
              const dataLine = chunk
                .split('\n')
                .map((line) => line.trim())
                .find((line) => line.startsWith('data:'));
              if (!dataLine) {
                continue;
              }

              const raw = dataLine.slice('data:'.length).trim();
              if (!raw || raw === '[DONE]') {
                continue;
              }

              const event = normalizeChatStreamEvent(JSON.parse(raw));
              subscriber.next(event);

              // Yield so zoneless CD can paint node-progress / streaming tokens
              // when multiple SSE frames arrive in one TCP read.
              await new Promise<void>((resolve) => setTimeout(resolve, 0));

              if (event.type === 'done' || event.type === 'error') {
                subscriber.complete();
                controller.abort();
                return;
              }
            }
          }

          subscriber.complete();
        } catch (error) {
          if (controller.signal.aborted) {
            subscriber.complete();
            return;
          }
          subscriber.error(error);
        }
      })();

      return () => controller.abort();
    });
  }

  rename(id: number, title: string): Observable<ConversationListItem> {
    return this.http.patch<ConversationListItem>(`${this.api}/conversations/${id}`, { title });
  }

  setPinned(id: number, isPinned: boolean): Observable<ConversationListItem> {
    return this.http.patch<ConversationListItem>(`${this.api}/conversations/${id}/pin`, { isPinned });
  }

  assignFolder(id: number, folderId: number | null): Observable<ConversationListItem> {
    return this.http.patch<ConversationListItem>(`${this.api}/conversations/${id}/folder`, { folderId });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/conversations/${id}`);
  }

  getFolders(): Observable<ConversationFolder[]> {
    return this.http.get<ConversationFolder[]>(`${this.api}/folders`);
  }

  createFolder(name: string): Observable<ConversationFolder> {
    return this.http.post<ConversationFolder>(`${this.api}/folders`, { name });
  }

  renameFolder(id: number, name: string): Observable<ConversationFolder> {
    return this.http.patch<ConversationFolder>(`${this.api}/folders/${id}`, { name });
  }

  deleteFolder(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/folders/${id}`);
  }

  uploadAttachment(file: File): Observable<AttachmentItem> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<AttachmentItem>(`${this.api}/attachments`, form);
  }
}

function normalizeChatStreamEvent(raw: Record<string, unknown>): ChatStreamEvent {
  const read = (camel: string, pascal: string): unknown =>
    raw[camel] !== undefined ? raw[camel] : raw[pascal];

  return {
    type: String(read('type', 'Type') ?? '').trim(),
    conversationId: (read('conversationId', 'ConversationId') as number | null | undefined) ?? null,
    title: (read('title', 'Title') as string | null | undefined) ?? null,
    userMessage: (read('userMessage', 'UserMessage') as ChatStreamEvent['userMessage']) ?? null,
    nodeId: (read('nodeId', 'NodeId') as number | null | undefined) ?? null,
    nodeName: (read('nodeName', 'NodeName') as string | null | undefined) ?? null,
    nodeType: (read('nodeType', 'NodeType') as string | null | undefined) ?? null,
    model: (read('model', 'Model') as string | null | undefined) ?? null,
    status: String(read('status', 'Status') ?? '').trim() || null,
    content: (read('content', 'Content') as string | null | undefined) ?? null,
    response: (read('response', 'Response') as ChatStreamEvent['response']) ?? null,
    message: (read('message', 'Message') as string | null | undefined) ?? null,
  };
}
