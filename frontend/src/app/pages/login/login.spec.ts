import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { appConfig } from '../../app.config';
import { Login } from './login';
describe('Login', () => { it('should create', async () => { await TestBed.configureTestingModule({ imports: [Login], providers: [...appConfig.providers, provideHttpClientTesting()] }).compileComponents(); expect(TestBed.createComponent(Login).componentInstance).toBeTruthy(); }); });
