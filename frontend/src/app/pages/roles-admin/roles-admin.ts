import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { AppPermissions } from '../../core/auth/permissions';
import { AdminRole } from '../../core/models/admin.model';
import { AdminService } from '../../core/services/admin.service';
import { AppDialogService } from '../../core/services/app-dialog.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { FieldError } from '../../shared/components/field-error/field-error';

const SYSTEM_ROLES = new Set(['admin', 'moderator', 'user']);

@Component({
  selector: 'app-roles-admin',
  imports: [ReactiveFormsModule, TranslatePipe, FieldError],
  templateUrl: './roles-admin.html',
  styleUrl: './roles-admin.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RolesAdmin implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(AppDialogService);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly roles = signal<AdminRole[]>([]);
  readonly allPermissions = signal<string[]>([]);
  readonly showForm = signal(false);
  readonly editing = signal<AdminRole | null>(null);
  readonly canManage = computed(() => this.auth.can(AppPermissions.RolesManage));

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    permissions: this.fb.nonNullable.control<string[]>([]),
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    forkJoin({
      roles: this.admin.getRoles(),
      permissions: this.admin.getPermissions(),
    }).subscribe({
      next: ({ roles, permissions }) => {
        this.roles.set(roles);
        this.allPermissions.set(permissions);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('toast.loadError');
      },
    });
  }

  isSystemRole(role: AdminRole | null): boolean {
    return !!role && SYSTEM_ROLES.has(role.name.toLowerCase());
  }

  openCreate(): void {
    this.editing.set(null);
    this.form.reset({ name: '', permissions: [] });
    this.form.controls.name.enable();
    this.showForm.set(true);
  }

  openEdit(role: AdminRole): void {
    this.editing.set(role);
    this.form.reset({
      name: role.name,
      permissions: [...role.permissions],
    });
    if (this.isSystemRole(role)) {
      this.form.controls.name.disable();
    } else {
      this.form.controls.name.enable();
    }
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editing.set(null);
  }

  togglePermission(permission: string, checked: boolean): void {
    const current = new Set(this.form.controls.permissions.value);
    if (checked) current.add(permission);
    else current.delete(permission);
    this.form.controls.permissions.setValue([...current].sort());
  }

  hasPermission(permission: string): boolean {
    return this.form.controls.permissions.value.some(
      (p) => p.toLowerCase() === permission.toLowerCase(),
    );
  }

  save(): void {
    if (!this.canManage()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const editing = this.editing();

    if (!editing) {
      this.admin.createRole(raw).subscribe({
        next: () => {
          this.toast.showSuccess('toast.roleCreated');
          this.closeForm();
          this.reload();
        },
        error: (err) => this.toast.error(err?.error?.message || 'toast.saveError'),
      });
      return;
    }

    this.admin.updateRole(editing.id, raw).subscribe({
      next: () => {
        this.toast.showSuccess('toast.roleUpdated');
        this.closeForm();
        this.reload();
      },
      error: (err) => this.toast.error(err?.error?.message || 'toast.saveError'),
    });
  }

  async remove(role: AdminRole): Promise<void> {
    if (!this.canManage() || this.isSystemRole(role)) return;
    const ok = await this.dialog.confirm({
      title: this.translate.instant('admin.roles.deleteTitle'),
      message: this.translate.instant('admin.roles.deleteText', { name: role.name }),
      confirmLabel: this.translate.instant('common.delete'),
      danger: true,
    });
    if (!ok) return;

    this.admin.deleteRole(role.id).subscribe({
      next: () => {
        this.toast.showSuccess('toast.roleDeleted');
        this.reload();
      },
      error: (err) => this.toast.error(err?.error?.message || 'toast.deleteError'),
    });
  }
}
