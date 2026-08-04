import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { appConfig } from '../../app.config';
import { Register } from './register';
describe('Register', () => { it('should create', async () => { await TestBed.configureTestingModule({ imports: [Register], providers: [...appConfig.providers, provideHttpClientTesting()] }).compileComponents(); expect(TestBed.createComponent(Register).componentInstance).toBeTruthy(); }); });
