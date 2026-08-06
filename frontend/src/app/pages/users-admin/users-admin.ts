import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { AppPermissions } from '../../core/auth/permissions';
import { AdminRole, AdminUser } from '../../core/models/admin.model';
import { AdminService } from '../../core/services/admin.service';
import { AppDialogService } from '../../core/services/app-dialog.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { FieldError } from '../../shared/components/field-error/field-error';

@Component({
  selector: 'app-users-admin',
  imports: [ReactiveFormsModule, TranslatePipe, FieldError],
  templateUrl: './users-admin.html',
  styleUrl: './users-admin.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersAdmin implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(AppDialogService);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly users = signal<AdminUser[]>([]);
  readonly roles = signal<AdminRole[]>([]);
  readonly showForm = signal(false);
  readonly editing = signal<AdminUser | null>(null);
  readonly canManage = computed(() => this.auth.can(AppPermissions.UsersManage));

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    password: [''],
    roles: this.fb.nonNullable.control<string[]>([]),
    isActive: [true],
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    forkJoin({
      users: this.admin.getUsers(),
      roles: this.admin.getRoles(),
    }).subscribe({
      next: ({ users, roles }) => {
        this.users.set(users);
        this.roles.set(roles);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('toast.loadError');
      },
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form.reset({
      email: '',
      firstName: '',
      lastName: '',
      password: '',
      roles: ['User'],
      isActive: true,
    });
    this.form.controls.password.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.controls.password.updateValueAndValidity();
    this.form.controls.email.enable();
    this.showForm.set(true);
  }

  openEdit(user: AdminUser): void {
    this.editing.set(user);
    this.form.reset({
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      password: '',
      roles: [...user.roles],
      isActive: !user.isDeleted,
    });
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
    this.form.controls.email.disable();
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editing.set(null);
  }

  toggleRole(roleName: string, checked: boolean): void {
    const current = new Set(this.form.controls.roles.value);
    if (checked) current.add(roleName);
    else current.delete(roleName);
    this.form.controls.roles.setValue([...current]);
  }

  hasRole(roleName: string): boolean {
    return this.form.controls.roles.value.some((r) => r.toLowerCase() === roleName.toLowerCase());
  }

  save(): void {
    if (!this.canManage()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    if (!raw.roles.length) {
      this.toast.error('admin.users.roleRequired');
      return;
    }

    const editing = this.editing();
    if (!editing) {
      this.admin
        .createUser({
          email: raw.email,
          password: raw.password,
          firstName: raw.firstName,
          lastName: raw.lastName,
          roles: raw.roles,
        })
        .subscribe({
          next: () => {
            this.toast.showSuccess('toast.userCreated');
            this.closeForm();
            this.reload();
          },
          error: (err) => this.toast.error(err?.error?.message || 'toast.saveError'),
        });
      return;
    }

    this.admin
      .updateUser(editing.id, {
        email: raw.email,
        firstName: raw.firstName,
        lastName: raw.lastName,
        isActive: raw.isActive,
        roles: raw.roles,
      })
      .subscribe({
        next: () => {
          this.toast.showSuccess('toast.userUpdated');
          this.closeForm();
          this.reload();
        },
        error: (err) => this.toast.error(err?.error?.message || 'toast.saveError'),
      });
  }

  async deactivate(user: AdminUser): Promise<void> {
    if (!this.canManage()) return;
    const ok = await this.dialog.confirm({
      title: this.translate.instant('admin.users.deactivateTitle'),
      message: this.translate.instant('admin.users.deactivateText', { email: user.email }),
      confirmLabel: this.translate.instant('admin.users.deactivate'),
      danger: true,
    });
    if (!ok) return;

    this.admin.deleteUser(user.id).subscribe({
      next: () => {
        this.toast.showSuccess('toast.userDeactivated');
        this.reload();
      },
      error: (err) => this.toast.error(err?.error?.message || 'toast.deleteError'),
    });
  }

  async reactivate(user: AdminUser): Promise<void> {
    if (!this.canManage()) return;
    this.admin
      .updateUser(user.id, {
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        isActive: true,
        roles: user.roles,
      })
      .subscribe({
        next: () => {
          this.toast.showSuccess('toast.userUpdated');
          this.reload();
        },
        error: (err) => this.toast.error(err?.error?.message || 'toast.saveError'),
      });
  }

  async resetPassword(user: AdminUser): Promise<void> {
    if (!this.canManage()) return;
    const password = await this.dialog.prompt({
      title: this.translate.instant('admin.users.resetPasswordTitle'),
      message: this.translate.instant('admin.users.resetPasswordText', { email: user.email }),
      placeholder: this.translate.instant('admin.users.newPassword'),
      confirmLabel: this.translate.instant('admin.users.resetPassword'),
    });
    if (!password) return;
    if (password.length < 8) {
      this.toast.error('admin.users.passwordMin');
      return;
    }

    this.admin.resetPassword(user.id, password).subscribe({
      next: () => this.toast.showSuccess('toast.passwordReset'),
      error: (err) => this.toast.error(err?.error?.message || 'toast.passwordError'),
    });
  }
}
