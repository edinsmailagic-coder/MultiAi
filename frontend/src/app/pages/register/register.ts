import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize, switchMap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Brand } from '../../shared/components/brand/brand';
import { FieldError } from '../../shared/components/field-error/field-error';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  return control.get('password')?.value === control.get('confirmPassword')?.value
    ? null
    : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, Brand, FieldError],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  readonly loading = signal(false);
  readonly currentLanguage = signal(localStorage.getItem('lang') ?? 'bs');
  readonly form;

  constructor(
    fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly translate: TranslateService,
    private readonly toast: ToastService,
  ) {
    this.form = fb.nonNullable.group(
      {
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: passwordsMatch },
    );
    this.translate.use(this.currentLanguage());
  }

  changeLanguage(lang: 'bs' | 'en'): void {
    this.currentLanguage.set(lang);
    localStorage.setItem('lang', lang);
    this.translate.use(lang);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { confirmPassword: _, ...request } = this.form.getRawValue();
    this.loading.set(true);
    this.auth
      .register(request)
      .pipe(
        switchMap(() => this.auth.loadCurrentUser()),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: () => {
          this.toast.showSuccess('toast.registerSuccess');
          void this.router.navigate(['/dashboard']);
        },
        error: () => this.toast.error('toast.registerError'),
      });
  }
}
