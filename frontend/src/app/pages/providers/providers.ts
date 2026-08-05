import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, switchMap } from 'rxjs';
import { AIProvider, ConnectedProvider } from '../../core/models/provider.model';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { ModelSelect } from '../../shared/components/model-select/model-select';
import { FieldError } from '../../shared/components/field-error/field-error';
import { AppSelect, AppSelectOption } from '../../shared/components/app-select/app-select';

@Component({
  selector: 'app-providers',
  imports: [FormsModule, ReactiveFormsModule, TranslatePipe, ModelSelect, FieldError, AppSelect],
  templateUrl: './providers.html',
  styleUrl: './providers.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Providers implements OnInit {
  readonly providers = signal<AIProvider[]>([]);
  readonly connected = signal<ConnectedProvider[]>([]);
  readonly defaultModelDrafts = signal<Record<number, string>>({});
  readonly providerForm;
  readonly selectedProviderId = signal('0');

  readonly providerOptions = computed<AppSelectOption[]>(() => [
    { value: '0', label: this.translate.instant('profile.selectProvider') },
    ...this.providers().map((provider) => ({
      value: String(provider.id),
      label: provider.name,
    })),
  ]);

  constructor(
    fb: FormBuilder,
    private readonly providerService: ProviderService,
    private readonly toast: ToastService,
    private readonly translate: TranslateService,
  ) {
    this.providerForm = fb.nonNullable.group({
      providerId: [0, [Validators.required, Validators.min(1)]],
      apiKey: ['', Validators.required],
    });
  }

  ngOnInit(): void {
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

  onProviderSelect(value: string): void {
    this.selectedProviderId.set(value);
    this.providerForm.controls.providerId.setValue(Number(value) || 0);
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
          this.providerForm.patchValue({ apiKey: '', providerId: 0 });
          this.selectedProviderId.set('0');
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

  private setConnected(connected: ConnectedProvider[]): void {
    this.connected.set(connected);
    const drafts: Record<number, string> = {};
    for (const item of connected) {
      drafts[item.id] = item.defaultModel ?? '';
    }
    this.defaultModelDrafts.set(drafts);
  }
}
