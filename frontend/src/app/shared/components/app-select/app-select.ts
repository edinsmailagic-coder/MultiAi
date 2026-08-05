import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  effect,
  forwardRef,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface AppSelectOption {
  value: string;
  label: string;
  hint?: string;
  disabled?: boolean;
}

interface PanelBox {
  top: number | null;
  bottom: number | null;
  left: number;
  width: number;
  maxHeight: number;
}

@Component({
  selector: 'app-select',
  templateUrl: './app-select.html',
  styleUrl: './app-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AppSelect),
      multi: true,
    },
  ],
  host: {
    class: 'app-select-host',
  },
})
export class AppSelect implements ControlValueAccessor {
  readonly options = input<AppSelectOption[]>([]);
  readonly placeholder = input('Select…');
  readonly searchable = input(false);
  readonly searchPlaceholder = input('Search…');
  readonly disabledInput = input(false, { alias: 'disabled' });
  readonly ariaLabel = input<string | null>(null);
  readonly valueInput = input<string | null>(null, { alias: 'value' });
  readonly valueChange = output<string>();

  readonly open = signal(false);
  readonly value = signal('');
  readonly isDisabled = signal(false);
  readonly query = signal('');
  readonly panelBox = signal<PanelBox | null>(null);

  readonly selectedLabel = computed(() => {
    const current = this.value();
    const match = this.options().find((option) => option.value === current);
    return match?.label || current || this.placeholder();
  });

  readonly filteredOptions = computed(() => {
    const q = this.query().trim().toLowerCase();
    const list = this.options();
    if (!q) {
      return list;
    }
    return list.filter(
      (option) =>
        option.label.toLowerCase().includes(q) ||
        option.value.toLowerCase().includes(q) ||
        (option.hint?.toLowerCase().includes(q) ?? false),
    );
  });

  private onChange: (value: string) => void = () => undefined;
  private onTouchedFn: () => void = () => undefined;
  private cvaWriting = false;

  constructor(private readonly host: ElementRef<HTMLElement>) {
    effect(() => {
      const incoming = this.valueInput();
      if (this.cvaWriting) {
        return;
      }
      if (incoming !== null && incoming !== undefined) {
        this.value.set(incoming);
      }
    });
  }

  @HostListener('document:pointerdown', ['$event'])
  onDocumentPointerDown(event: PointerEvent): void {
    if (!this.open()) {
      return;
    }
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.close();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) {
      this.close();
    }
  }

  @HostListener('window:resize')
  onResize(): void {
    if (this.open()) {
      this.updatePanelPosition();
    }
  }

  @HostListener('window:scroll')
  onScroll(): void {
    if (this.open()) {
      this.updatePanelPosition();
    }
  }

  writeValue(value: string | null): void {
    this.cvaWriting = true;
    this.value.set(value ?? '');
    queueMicrotask(() => {
      this.cvaWriting = false;
    });
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

  toggle(): void {
    if (this.isDisabled() || this.disabledInput()) {
      return;
    }
    if (this.open()) {
      this.close();
      return;
    }
    this.query.set('');
    this.updatePanelPosition();
    this.open.set(true);
  }

  select(option: AppSelectOption): void {
    if (option.disabled) {
      return;
    }
    this.value.set(option.value);
    this.onChange(option.value);
    this.valueChange.emit(option.value);
    this.onTouchedFn();
    this.close();
  }

  onSearchInput(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  isSelected(option: AppSelectOption): boolean {
    return option.value === this.value();
  }

  private updatePanelPosition(): void {
    const trigger = this.host.nativeElement.querySelector('.app-select__trigger') as HTMLElement | null;
    if (!trigger) {
      return;
    }

    const rect = trigger.getBoundingClientRect();
    const gap = 6;
    const pad = 8;
    const preferred = 320;
    const spaceBelow = window.innerHeight - rect.bottom - pad;
    const spaceAbove = rect.top - pad;
    const openUp = spaceBelow < 220 && spaceAbove > spaceBelow;
    const available = Math.max(120, openUp ? spaceAbove - gap : spaceBelow - gap);
    const maxHeight = Math.min(preferred, available);
    const width = Math.max(rect.width, 200);

    let left = rect.left;
    if (left + width > window.innerWidth - pad) {
      left = Math.max(pad, window.innerWidth - width - pad);
    }
    left = Math.max(pad, left);

    // Anchor to trigger edge so short menus don't float away from the control.
    if (openUp) {
      this.panelBox.set({
        top: null,
        bottom: window.innerHeight - rect.top + gap,
        left,
        width,
        maxHeight,
      });
    } else {
      this.panelBox.set({
        top: rect.bottom + gap,
        bottom: null,
        left,
        width,
        maxHeight,
      });
    }
  }

  private close(): void {
    this.open.set(false);
    this.query.set('');
    this.panelBox.set(null);
    this.onTouchedFn();
  }
}
