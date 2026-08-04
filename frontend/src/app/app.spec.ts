import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { appConfig } from './app.config';
import { App } from './app';
describe('App', () => { it('should create the app shell outlet', async () => { await TestBed.configureTestingModule({ imports: [App], providers: [...appConfig.providers, provideHttpClientTesting()] }).compileComponents(); const fixture = TestBed.createComponent(App); fixture.detectChanges(); expect(fixture.componentInstance).toBeTruthy(); expect((fixture.nativeElement as HTMLElement).querySelector('router-outlet')).not.toBeNull(); }); });
