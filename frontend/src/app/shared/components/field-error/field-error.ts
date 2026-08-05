import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-field-error',
  imports: [TranslatePipe],
  templateUrl: './field-error.html',
  styleUrl: './field-error.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldError {
  /** Form control to inspect. */
  readonly control = input<AbstractControl | null>(null);
  /** i18n key for the field label shown in the error message. */
  readonly fieldKey = input.required<string>();

  get show(): boolean {
    const control = this.control();
    return Boolean(control && (control.touched || control.dirty) && control.invalid);
  }

  get minLength(): number {
    return Number(this.control()?.getError('minlength')?.requiredLength ?? 0);
  }
}
