import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { UserService } from '../../core/services/user.service';
import { FieldError } from '../../shared/components/field-error/field-error';

@Component({
  selector: 'app-settings',
  imports: [ReactiveFormsModule, TranslatePipe, FieldError],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings implements OnInit {
  private readonly translate = inject(TranslateService);

  readonly currentLang = signal(localStorage.getItem('lang') ?? 'bs');
  readonly passwordForm;

  constructor(
    fb: FormBuilder,
    readonly auth: AuthService,
    private readonly userService: UserService,
    private readonly toast: ToastService,
  ) {
    this.passwordForm = fb.nonNullable.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  ngOnInit(): void {
    const active = this.resolveCurrentLang();
    if (active) {
      this.currentLang.set(active);
    }
  }

  setLanguage(lang: 'bs' | 'en'): void {
    if (this.currentLang() === lang) {
      return;
    }
    localStorage.setItem('lang', lang);
    this.translate.use(lang);
    this.currentLang.set(lang);
    this.toast.showSuccess('toast.languageChanged');
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.userService.changePassword(this.passwordForm.getRawValue()).subscribe({
      next: () => {
        this.passwordForm.reset();
        this.toast.showSuccess('toast.passwordChanged');
      },
      error: () => this.toast.error('toast.passwordError'),
    });
  }

  private resolveCurrentLang(): string {
    const service = this.translate as TranslateService & {
      getCurrentLang?: () => string | null;
      currentLang: string | null | (() => string | null);
    };

    if (typeof service.getCurrentLang === 'function') {
      return service.getCurrentLang() || localStorage.getItem('lang') || 'bs';
    }

    const current = service.currentLang;
    const value = typeof current === 'function' ? current() : current;
    return value || localStorage.getItem('lang') || 'bs';
  }
}
