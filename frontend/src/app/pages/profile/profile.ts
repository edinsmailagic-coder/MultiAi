import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { switchMap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { UserService } from '../../core/services/user.service';
import { FieldError } from '../../shared/components/field-error/field-error';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, TranslatePipe, FieldError],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Profile implements OnInit {
  readonly profileForm;

  constructor(
    fb: FormBuilder,
    readonly auth: AuthService,
    private readonly userService: UserService,
    private readonly toast: ToastService,
  ) {
    this.profileForm = fb.nonNullable.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (user) {
      this.profileForm.patchValue(user);
    } else {
      this.auth.loadCurrentUser().subscribe((value) => this.profileForm.patchValue(value));
    }
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
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
}
