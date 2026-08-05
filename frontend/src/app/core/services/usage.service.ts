import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UsageStats } from '../models/usage.model';

@Injectable({ providedIn: 'root' })
export class UsageService {
  private readonly api = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getDashboardStats(): Observable<UsageStats> {
    return this.http.get<UsageStats>(`${this.api}/api/usage/dashboard`);
  }
}
