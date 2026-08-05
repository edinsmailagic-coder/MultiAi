import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Workflow } from '../../../core/models/workflow.model';
import { AppDialogService } from '../../../core/services/app-dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { WorkflowService } from '../../../core/services/workflow.service';
import { FieldError } from '../../../shared/components/field-error/field-error';

@Component({
  selector: 'app-workflow-list',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, FieldError],
  templateUrl: './workflow-list.html',
  styleUrl: './workflow-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowList implements OnInit {
  readonly workflows = signal<Workflow[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);
  readonly createForm;

  private readonly service = inject(WorkflowService);
  private readonly toast = inject(ToastService);
  private readonly dialogs = inject(AppDialogService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);

  constructor(fb: FormBuilder) {
    this.createForm = fb.nonNullable.group({
      name: ['', Validators.required],
      description: [''],
    });
  }

  ngOnInit(): void {
    this.service.getAll().subscribe({
      next: (rows) => {
        this.workflows.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('toast.loadError');
      },
    });
  }

  create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const value = this.createForm.getRawValue();
    this.service.create(value.name, value.description).subscribe({
      next: (workflow) => {
        this.workflows.update((items) => [workflow, ...items]);
        this.showCreate.set(false);
        this.createForm.reset();
        this.toast.showSuccess('toast.workflowCreated');
        void this.router.navigate(['/workflows', workflow.id]);
      },
      error: () => this.toast.error('toast.saveError'),
    });
  }

  async delete(item: Workflow, event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const ok = await this.dialogs.confirm({
      title: this.translate.instant('workflow.deleteTitle'),
      message: this.translate.instant('workflow.deleteText', { name: item.name }),
    });
    if (!ok) {
      return;
    }
    this.service.delete(item.id).subscribe({
      next: () => this.workflows.update((rows) => rows.filter((row) => row.id !== item.id)),
      error: () => this.toast.error('toast.deleteError'),
    });
  }
}
