import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChangePasswordRequest, UpdateProfileRequest, User } from '../models/user.model';
@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly http: HttpClient) {}
  updateProfile(data: UpdateProfileRequest): Observable<User> { return this.http.put<User>(`${environment.apiUrl}/api/auth/profile`, data); }
  changePassword(data: ChangePasswordRequest): Observable<void> { return this.http.put<void>(`${environment.apiUrl}/api/auth/password`, data); }
}
