import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AttachmentItem,
  ConversationDetail,
  ConversationFolder,
  ConversationListItem,
  SendChatRequest,
  SendChatResult,
} from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly api = `${environment.apiUrl}/api/chat`;

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
