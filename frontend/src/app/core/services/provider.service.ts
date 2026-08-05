import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AIModel, AIProvider, ConnectedProvider, ConnectProviderRequest } from '../models/provider.model';

@Injectable({ providedIn: 'root' })
export class ProviderService {
  private readonly api = `${environment.apiUrl}/api/providers`;

  constructor(private readonly http: HttpClient) {}

  getProviders(): Observable<AIProvider[]> {
    return this.http.get<AIProvider[]>(this.api);
  }

  getConnected(): Observable<ConnectedProvider[]> {
    return this.http.get<ConnectedProvider[]>(`${this.api}/connected`);
  }

  connect(data: ConnectProviderRequest): Observable<ConnectedProvider> {
    return this.http.post<ConnectedProvider>(`${this.api}/connect`, data);
  }

  getModels(userProviderId: number): Observable<AIModel[]> {
    return this.http.get<AIModel[]>(`${this.api}/connected/${userProviderId}/models`);
  }

  setDefaultModel(userProviderId: number, defaultModel: string): Observable<ConnectedProvider> {
    return this.http.put<ConnectedProvider>(`${this.api}/connected/${userProviderId}/default-model`, {
      defaultModel,
    });
  }

  setDefaultProvider(userProviderId: number): Observable<ConnectedProvider> {
    return this.http.put<ConnectedProvider>(`${this.api}/connected/${userProviderId}/set-default`, {});
  }
}
