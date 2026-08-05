import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  forwardRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { AIModel } from '../../../core/models/provider.model';
import { ProviderService } from '../../../core/services/provider.service';

@Component({
  selector: 'app-model-select',
  imports: [TranslatePipe],
  templateUrl: './model-select.html',
  styleUrl: './model-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ModelSelect),
      multi: true,
    },
  ],
})
export class ModelSelect implements ControlValueAccessor, OnDestroy {
  private readonly providerService = inject(ProviderService);

  readonly userProviderId = input<number | null>(null);
  readonly defaultModel = input<string | null | undefined>(null);
  readonly supportsModelListing = input<boolean>(true);
  readonly disabledInput = input(false, { alias: 'disabled' });

  readonly models = signal<AIModel[]>([]);
  readonly loading = signal(false);
  readonly loadFailed = signal(false);
  readonly value = signal('');
  readonly isDisabled = signal(false);
  readonly useManualInput = computed(
    () =>
      !this.supportsModelListing() ||
      this.loadFailed() ||
      (!this.loading() && this.models().length === 0),
  );

  private onChange: (value: string) => void = () => undefined;
  private onTouchedFn: () => void = () => undefined;
  private loadSub?: Subscription;
  private lastProviderId: number | null = null;
  private preferDefault = true;

  constructor() {
    effect(() => {
      const providerId = this.userProviderId();
      const listing = this.supportsModelListing();
      if (providerId == null || providerId <= 0) {
        this.models.set([]);
        this.loadFailed.set(false);
        this.lastProviderId = null;
        return;
      }

      if (providerId !== this.lastProviderId) {
        this.lastProviderId = providerId;
        this.preferDefault = true;
        this.loadModels(providerId, listing);
      }
    });

    effect(() => {
      this.isDisabled.set(this.disabledInput());
    });
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
  }

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
    if (value) {
      this.preferDefault = false;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedFn = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }

  onSelectChange(event: Event): void {
    const next = (event.target as HTMLSelectElement | HTMLInputElement).value;
    this.preferDefault = false;
    this.value.set(next);
    this.onChange(next);
    this.onTouchedFn();
  }

  markTouched(): void {
    this.onTouchedFn();
  }

  private loadModels(providerId: number, listing: boolean): void {
    this.loadSub?.unsubscribe();

    if (!listing) {
      this.models.set([]);
      this.loadFailed.set(false);
      this.applyDefaultIfNeeded();
      return;
    }

    this.loading.set(true);
    this.loadFailed.set(false);
    this.loadSub = this.providerService.getModels(providerId).subscribe({
      next: (models) => {
        this.models.set(models);
        this.loading.set(false);
        this.applyDefaultIfNeeded();
      },
      error: () => {
        this.models.set([]);
        this.loading.set(false);
        this.loadFailed.set(true);
        this.applyDefaultIfNeeded();
      },
    });
  }

  private applyDefaultIfNeeded(): void {
    if (!this.preferDefault) {
      return;
    }

    const preferred = this.defaultModel()?.trim();
    if (preferred) {
      this.value.set(preferred);
      this.onChange(preferred);
      return;
    }

    if (!this.value() && this.models().length > 0) {
      const first = this.models()[0].id;
      this.value.set(first);
      this.onChange(first);
    }
  }
}
