import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, switchMap } from 'rxjs';
import { AIProvider, ConnectedProvider } from '../../core/models/provider.model';
import { AuthService } from '../../core/services/auth.service';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { UserService } from '../../core/services/user.service';
import { ModelSelect } from '../../shared/components/model-select/model-select';

@Component({
  selector: 'app-profile',
  imports: [FormsModule, ReactiveFormsModule, TranslatePipe, ModelSelect],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Profile implements OnInit {
  private readonly translate = inject(TranslateService);

  readonly providers = signal<AIProvider[]>([]);
  readonly connected = signal<ConnectedProvider[]>([]);
  readonly currentLang = signal(localStorage.getItem('lang') ?? 'bs');
  readonly profileForm;
  readonly passwordForm;
  readonly providerForm;
  readonly defaultModelDrafts = signal<Record<number, string>>({});

  constructor(
    fb: FormBuilder,
    readonly auth: AuthService,
    private readonly userService: UserService,
    private readonly providerService: ProviderService,
    private readonly toast: ToastService,
  ) {
    this.profileForm = fb.nonNullable.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
    });
    this.passwordForm = fb.nonNullable.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
    });
    this.providerForm = fb.nonNullable.group({
      providerId: [0, [Validators.required, Validators.min(1)]],
      apiKey: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    const active = this.resolveCurrentLang();
    if (active) {
      this.currentLang.set(active);
    }

    const user = this.auth.currentUser();
    if (user) {
      this.profileForm.patchValue(user);
    } else {
      this.auth.loadCurrentUser().subscribe((value) => this.profileForm.patchValue(value));
    }

    forkJoin({
      providers: this.providerService.getProviders(),
      connected: this.providerService.getConnected(),
    }).subscribe({
      next: (data) => {
        this.providers.set(data.providers);
        this.setConnected(data.connected);
      },
      error: () => this.toast.error('toast.loadError'),
    });
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

  saveProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.userService
      .updateProfile(this.profileForm.getRawValue())
      .pipe(switchMap(() => this.auth.loadCurrentUser()))
      .subscribe({
        next: () => this.toast.showSuccess('toast.profileSaved'),
        error: () => this.toast.error('toast.saveError'),
      });
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

  connect(): void {
    if (this.providerForm.invalid) {
      this.providerForm.markAllAsTouched();
      return;
    }

    const raw = this.providerForm.getRawValue();
    this.providerService
      .connect({ providerId: Number(raw.providerId), apiKey: raw.apiKey })
      .pipe(switchMap(() => this.providerService.getConnected()))
      .subscribe({
        next: (connected) => {
          this.setConnected(connected);
          this.providerForm.patchValue({ apiKey: '' });
          this.toast.showSuccess('toast.providerConnected');
        },
        error: () => this.toast.error('toast.providerError'),
      });
  }

  onDefaultModelChange(providerId: number, model: string): void {
    this.defaultModelDrafts.update((drafts) => ({ ...drafts, [providerId]: model }));
  }

  saveDefaultModel(item: ConnectedProvider): void {
    const model = (this.defaultModelDrafts()[item.id] ?? item.defaultModel ?? '').trim();
    if (!model) {
      this.toast.error('toast.saveError');
      return;
    }

    this.providerService.setDefaultModel(item.id, model).subscribe({
      next: (updated) => {
        this.connected.update((list) =>
          list.map((c) =>
            c.id === updated.id
              ? {
                  ...c,
                  ...updated,
                  providerName: updated.providerName ?? c.providerName,
                }
              : c,
          ),
        );
        this.defaultModelDrafts.update((drafts) => ({
          ...drafts,
          [item.id]: updated.defaultModel ?? model,
        }));
        this.toast.showSuccess('toast.defaultModelSaved');
      },
      error: () => this.toast.error('toast.saveError'),
    });
  }

  setAsDefaultProvider(item: ConnectedProvider): void {
    this.providerService.setDefaultProvider(item.id).subscribe({
      next: () => {
        this.connected.update((list) =>
          list.map((c) => ({
            ...c,
            isDefault: c.id === item.id,
          })),
        );
        this.toast.showSuccess('toast.defaultProviderSaved');
      },
      error: () => this.toast.error('toast.saveError'),
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

  private setConnected(connected: ConnectedProvider[]): void {
    this.connected.set(connected);
    const drafts: Record<number, string> = {};
    for (const item of connected) {
      drafts[item.id] = item.defaultModel ?? '';
    }
    this.defaultModelDrafts.set(drafts);
  }
}
