import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
describe('AuthService', () => { let service: AuthService; let http: HttpTestingController; beforeEach(() => { localStorage.clear(); TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([])] }); service = TestBed.inject(AuthService); http = TestBed.inject(HttpTestingController); }); afterEach(() => http.verify()); it('stores token returned by login', () => { service.login({ email: 'user@example.com', password: 'password' }).subscribe(); http.expectOne(`${environment.apiUrl}/api/auth/login`).flush({ token: 'jwt-token' }); expect(localStorage.getItem('multiAiToken')).toBe('jwt-token'); expect(service.isAuthenticated()).toBeTrue(); }); it('clears token on logout', () => { localStorage.setItem('multiAiToken', 'token'); service.logout(); expect(localStorage.getItem('multiAiToken')).toBeNull(); }); });
