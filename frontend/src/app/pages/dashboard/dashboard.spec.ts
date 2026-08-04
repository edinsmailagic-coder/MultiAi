import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { appConfig } from '../../app.config';
import { Dashboard } from './dashboard';
describe('Dashboard', () => { it('should create', async () => { await TestBed.configureTestingModule({ imports: [Dashboard], providers: [...appConfig.providers, provideHttpClientTesting()] }).compileComponents(); expect(TestBed.createComponent(Dashboard).componentInstance).toBeTruthy(); }); });
