import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminRole,
  AdminUser,
  CreateAdminUserRequest,
  UpdateAdminUserRequest,
  UpsertRoleRequest,
} from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${environment.apiUrl}/api/admin/users`);
  }

  createUser(payload: CreateAdminUserRequest): Observable<AdminUser> {
    return this.http.post<AdminUser>(`${environment.apiUrl}/api/admin/users`, payload);
  }

  updateUser(id: string, payload: UpdateAdminUserRequest): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${environment.apiUrl}/api/admin/users/${id}`, payload);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/api/admin/users/${id}`);
  }

  resetPassword(id: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/api/admin/users/${id}/password`, { newPassword });
  }

  getRoles(): Observable<AdminRole[]> {
    return this.http.get<AdminRole[]>(`${environment.apiUrl}/api/admin/roles`);
  }

  getPermissions(): Observable<string[]> {
    return this.http.get<string[]>(`${environment.apiUrl}/api/admin/roles/permissions`);
  }

  createRole(payload: UpsertRoleRequest): Observable<AdminRole> {
    return this.http.post<AdminRole>(`${environment.apiUrl}/api/admin/roles`, payload);
  }

  updateRole(id: string, payload: UpsertRoleRequest): Observable<AdminRole> {
    return this.http.put<AdminRole>(`${environment.apiUrl}/api/admin/roles/${id}`, payload);
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/api/admin/roles/${id}`);
  }
}
