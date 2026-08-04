import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';
describe('authGuard', () => { it('redirects guests to login', () => { const auth = { isAuthenticated: () => false } as Pick<AuthService, 'isAuthenticated'>; TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection(), provideRouter([]), { provide: AuthService, useValue: auth }] }); const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never)); expect(result instanceof UrlTree).toBeTrue(); expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login'); }); });
