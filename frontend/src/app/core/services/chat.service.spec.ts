import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ChatService } from './chat.service';

describe('ChatService', () => {
  let service: ChatService;
  let http: HttpTestingController;
  const api = `${environment.apiUrl}/api/chat`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ChatService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists conversations', () => {
    let resultLength = -1;
    service.getConversations().subscribe((rows) => (resultLength = rows.length));
    http.expectOne(`${api}/conversations`).flush([{ id: 1, title: 'Hello', createdAt: '2026-01-01' }]);
    expect(resultLength).toBe(1);
  });

  it('sends a direct chat message', () => {
    service
      .send({
        message: 'Hi',
        userProviderId: 3,
        model: 'gpt-4o-mini',
      })
      .subscribe();

    const req = http.expectOne(`${api}/sendChat`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      message: 'Hi',
      userProviderId: 3,
      model: 'gpt-4o-mini',
    });
    req.flush({
      conversationId: 9,
      title: 'Hi',
      userMessage: { id: 1, content: 'Hi', createdAt: '2026-01-01' },
      response: {
        id: 2,
        model: 'gpt-4o-mini',
        content: 'Hello',
        promptTokens: 1,
        completitionTokens: 1,
        totalTokens: 2,
        durationMs: 10,
        createdAt: '2026-01-01',
      },
    });
  });

  it('sends a workflow chat message', () => {
    service
      .send({
        conversationId: 4,
        message: 'Run this',
        workflowId: 7,
      })
      .subscribe();

    const req = http.expectOne(`${api}/sendChat`);
    expect(req.request.body.workflowId).toBe(7);
    expect(req.request.body.conversationId).toBe(4);
    req.flush({
      conversationId: 4,
      title: 'Run',
      userMessage: { id: 3, content: 'Run this', createdAt: '2026-01-01' },
      response: {
        id: 4,
        model: 'workflow',
        content: 'done',
        promptTokens: 0,
        completitionTokens: 0,
        totalTokens: 0,
        durationMs: 20,
        createdAt: '2026-01-01',
      },
    });
  });

  it('renames and deletes conversations', () => {
    service.rename(5, 'Renamed').subscribe();
    const patch = http.expectOne(`${api}/conversations/5`);
    expect(patch.request.method).toBe('PATCH');
    patch.flush({ id: 5, title: 'Renamed', createdAt: '2026-01-01' });

    service.delete(5).subscribe();
    const del = http.expectOne(`${api}/conversations/5`);
    expect(del.request.method).toBe('DELETE');
    del.flush(null);
  });
});
