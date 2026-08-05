import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { catchError, forkJoin, of } from 'rxjs';
import { UsageStats } from '../../core/models/usage.model';
import { Workflow } from '../../core/models/workflow.model';
import { AuthService } from '../../core/services/auth.service';
import { AppDialogService } from '../../core/services/app-dialog.service';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { UsageService } from '../../core/services/usage.service';
import { WorkflowService } from '../../core/services/workflow.service';

const emptyUsage: UsageStats = {
  costByProvider: [],
  tokensByProvider: [],
  mostUsedWorkflowModels: [],
};

@Component({
  selector: 'app-dashboard',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, CurrencyPipe, DecimalPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard implements OnInit {
  readonly workflows = signal<Workflow[]>([]);
  readonly connectedCount = signal(0);
  readonly loading = signal(true);
  readonly showCreate = signal(false);
  readonly usage = signal<UsageStats | null>(null);
  readonly createForm;
  readonly activeCount = computed(() => this.workflows().filter((w) => w.isActive).length);

  constructor(
    fb: FormBuilder,
    readonly auth: AuthService,
    private readonly workflowService: WorkflowService,
    private readonly providerService: ProviderService,
    private readonly usageService: UsageService,
    private readonly toast: ToastService,
    private readonly dialogs: AppDialogService,
    private readonly translate: TranslateService,
    private readonly router: Router,
  ) {
    this.createForm = fb.nonNullable.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    forkJoin({
      workflows: this.workflowService.getAll(),
      providers: this.providerService.getConnected(),
      usage: this.usageService.getDashboardStats().pipe(catchError(() => of(emptyUsage))),
    }).subscribe({
      next: (data) => {
        this.workflows.set(data.workflows);
        this.connectedCount.set(data.providers.length);
        this.usage.set(data.usage);
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
    this.workflowService.create(value.name, value.description).subscribe({
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

  async delete(workflow: Workflow, event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const ok = await this.dialogs.confirm({
      title: this.translate.instant('workflow.deleteTitle'),
      message: this.translate.instant('workflow.deleteText', { name: workflow.name }),
    });
    if (!ok) {
      return;
    }
    this.workflowService.delete(workflow.id).subscribe({
      next: () => {
        this.workflows.update((items) => items.filter((item) => item.id !== workflow.id));
        this.toast.showSuccess('toast.workflowDeleted');
      },
      error: () => this.toast.error('toast.deleteError'),
    });
  }
}
