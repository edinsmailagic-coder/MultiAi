import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize, switchMap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
@Component({ selector: 'app-login', imports: [ReactiveFormsModule, RouterLink, TranslatePipe], templateUrl: './login.html', styleUrl: './login.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class Login {
  readonly loading = signal(false);
  readonly form;
  currentLanguage = signal(localStorage.getItem('lang') ?? 'bs');
  constructor(fb: FormBuilder, private readonly auth: AuthService, private readonly router: Router, private readonly translate: TranslateService, private readonly toast: ToastService) {
    this.form = fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required, Validators.minLength(6)]] });
    this.translate.use(this.currentLanguage());
  }
  changeLanguage(lang: 'bs' | 'en'): void { this.currentLanguage.set(lang); localStorage.setItem('lang', lang); this.translate.use(lang); }
  submit(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.loading.set(true); this.auth.login(this.form.getRawValue()).pipe(switchMap(() => this.auth.loadCurrentUser()), finalize(() => this.loading.set(false))).subscribe({ next: () => { this.toast.showSuccess('toast.loginSuccess'); void this.router.navigate(['/dashboard']); }, error: () => this.toast.error('toast.loginError') }); }
}
