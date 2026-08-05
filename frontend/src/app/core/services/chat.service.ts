import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ConversationDetail,
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

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/conversations/${id}`);
  }
}
