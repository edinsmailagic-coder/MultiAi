import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ProviderService } from './provider.service';

describe('ProviderService', () => {
  let service: ProviderService;
  let http: HttpTestingController;
  const api = `${environment.apiUrl}/api/providers`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProviderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads models for a connected provider', () => {
    service.getModels(12).subscribe();
    const req = http.expectOne(`${api}/connected/12/models`);
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'gpt-4o-mini', name: 'gpt-4o-mini' }]);
  });

  it('sets default model and default provider', () => {
    service.setDefaultModel(12, 'gpt-4o').subscribe();
    const modelReq = http.expectOne(`${api}/connected/12/default-model`);
    expect(modelReq.request.method).toBe('PUT');
    expect(modelReq.request.body).toEqual({ defaultModel: 'gpt-4o' });
    modelReq.flush({ id: 12, providerId: 1, providerName: 'OpenAI', isDefault: true, defaultModel: 'gpt-4o' });

    service.setDefaultProvider(12).subscribe();
    const defaultReq = http.expectOne(`${api}/connected/12/set-default`);
    expect(defaultReq.request.method).toBe('PUT');
    defaultReq.flush({ id: 12, providerId: 1, providerName: 'OpenAI', isDefault: true, defaultModel: 'gpt-4o' });
  });
});
