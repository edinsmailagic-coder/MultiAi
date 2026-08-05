import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AppPermission } from '../auth/permissions';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'multiAiToken';
  private readonly tokenState = signal<string | null>(localStorage.getItem(this.storageKey));
  readonly token = this.tokenState.asReadonly();
  readonly currentUser = signal<User | null>(null);
  readonly isAuthenticated = computed(() => Boolean(this.tokenState()));
  readonly permissions = computed(() => this.currentUser()?.permissions ?? []);
  readonly roles = computed(() => this.currentUser()?.roles ?? []);

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, credentials)
      .pipe(tap((response) => this.storeSession(response.token)));
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, data)
      .pipe(tap((response) => this.storeSession(response.token)));
  }

  loadCurrentUser(): Observable<User> {
    return this.http
      .get<User>(`${environment.apiUrl}/api/auth/me`)
      .pipe(tap((user) => this.currentUser.set(user)));
  }

  can(permission: AppPermission | string): boolean {
    return this.permissions().includes(permission);
  }

  hasRole(role: string): boolean {
    return this.roles().some((r) => r.toLowerCase() === role.toLowerCase());
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.tokenState.set(null);
    this.currentUser.set(null);
    void this.router.navigate(['/']);
  }

  private storeSession(token: string): void {
    localStorage.setItem(this.storageKey, token);
    this.tokenState.set(token);
  }
}
