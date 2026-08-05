import { ChangeDetectionStrategy, Component, ElementRef, HostListener, ViewChild, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AppDialogService } from '../../../core/services/app-dialog.service';

@Component({
  selector: 'app-dialog-host',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './dialog-host.html',
  styleUrl: './dialog-host.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DialogHost {
  readonly dialogs = inject(AppDialogService);

  @ViewChild('promptInput') private promptInput?: ElementRef<HTMLInputElement>;

  constructor() {
    effect(() => {
      const dialog = this.dialogs.dialog();
      if (dialog?.kind === 'prompt') {
        queueMicrotask(() => this.promptInput?.nativeElement.focus());
      }
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.dialogs.dialog()) {
      this.dialogs.dismiss();
    }
  }
}
