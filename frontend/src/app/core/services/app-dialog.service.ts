import { Injectable, signal } from '@angular/core';

export interface ConfirmDialogOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

export interface PromptDialogOptions {
  title: string;
  message?: string;
  defaultValue?: string;
  placeholder?: string;
  confirmLabel?: string;
  cancelLabel?: string;
}

export type AppDialogState =
  | ({ kind: 'confirm' } & ConfirmDialogOptions)
  | ({ kind: 'prompt'; value: string } & PromptDialogOptions);

@Injectable({ providedIn: 'root' })
export class AppDialogService {
  private readonly state = signal<AppDialogState | null>(null);
  private resolveConfirm: ((value: boolean) => void) | null = null;
  private resolvePrompt: ((value: string | null) => void) | null = null;

  readonly dialog = this.state.asReadonly();

  confirm(options: ConfirmDialogOptions): Promise<boolean> {
    this.rejectOpen();
    this.state.set({ kind: 'confirm', danger: true, ...options });
    return new Promise<boolean>((resolve) => {
      this.resolveConfirm = resolve;
    });
  }

  prompt(options: PromptDialogOptions): Promise<string | null> {
    this.rejectOpen();
    this.state.set({
      kind: 'prompt',
      value: options.defaultValue ?? '',
      ...options,
    });
    return new Promise<string | null>((resolve) => {
      this.resolvePrompt = resolve;
    });
  }

  setPromptValue(value: string): void {
    const current = this.state();
    if (current?.kind !== 'prompt') {
      return;
    }
    this.state.set({ ...current, value });
  }

  accept(): void {
    const current = this.state();
    if (!current) {
      return;
    }
    if (current.kind === 'confirm') {
      this.resolveConfirm?.(true);
    } else {
      this.resolvePrompt?.(current.value.trim() || null);
    }
    this.clear();
  }

  dismiss(): void {
    const current = this.state();
    if (!current) {
      return;
    }
    if (current.kind === 'confirm') {
      this.resolveConfirm?.(false);
    } else {
      this.resolvePrompt?.(null);
    }
    this.clear();
  }

  private rejectOpen(): void {
    if (this.resolveConfirm) {
      this.resolveConfirm(false);
    }
    if (this.resolvePrompt) {
      this.resolvePrompt(null);
    }
    this.clear();
  }

  private clear(): void {
    this.resolveConfirm = null;
    this.resolvePrompt = null;
    this.state.set(null);
  }
}
