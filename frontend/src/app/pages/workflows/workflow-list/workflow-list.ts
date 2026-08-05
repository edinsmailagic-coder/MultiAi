import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Workflow } from '../../../core/models/workflow.model';
import { ToastService } from '../../../core/services/toast.service';
import { WorkflowService } from '../../../core/services/workflow.service';

@Component({
  selector: 'app-workflow-list',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './workflow-list.html',
  styleUrl: './workflow-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowList implements OnInit {
  readonly workflows = signal<Workflow[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);
  readonly createForm;

  constructor(
    fb: FormBuilder,
    private readonly service: WorkflowService,
    private readonly toast: ToastService,
    private readonly router: Router,
  ) {
    this.createForm = fb.nonNullable.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
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

  delete(item: Workflow, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (!confirm(`${item.name}?`)) {
      return;
    }
    this.service.delete(item.id).subscribe({
      next: () => this.workflows.update((rows) => rows.filter((row) => row.id !== item.id)),
      error: () => this.toast.error('toast.deleteError'),
    });
  }
}
