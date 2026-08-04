import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AIProvider, ConnectedProvider, ConnectProviderRequest } from '../models/provider.model';
@Injectable({ providedIn: 'root' })
export class ProviderService {
  constructor(private readonly http: HttpClient) {}
  getProviders(): Observable<AIProvider[]> { return this.http.get<AIProvider[]>(`${environment.apiUrl}/api/providers`); }
  getConnected(): Observable<ConnectedProvider[]> { return this.http.get<ConnectedProvider[]>(`${environment.apiUrl}/api/providers/connected`); }
  connect(data: ConnectProviderRequest): Observable<ConnectedProvider> { return this.http.post<ConnectedProvider>(`${environment.apiUrl}/api/providers/connect`, data); }
}
