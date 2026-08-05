import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { Workflow } from '../../core/models/workflow.model';
import { AuthService } from '../../core/services/auth.service';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { WorkflowService } from '../../core/services/workflow.service';
@Component({ selector: 'app-dashboard', imports: [ReactiveFormsModule, RouterLink, TranslatePipe], templateUrl: './dashboard.html', styleUrl: './dashboard.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class Dashboard implements OnInit { readonly workflows = signal<Workflow[]>([]); readonly connectedCount = signal(0); readonly loading = signal(true); readonly showCreate = signal(false); readonly createForm; readonly activeCount = computed(() => this.workflows().filter(w => w.isActive).length); constructor(fb: FormBuilder, readonly auth: AuthService, private readonly workflowService: WorkflowService, private readonly providerService: ProviderService, private readonly toast: ToastService, private readonly router: Router) { this.createForm = fb.nonNullable.group({ name: ['', Validators.required], description: ['', Validators.required] }); }
ngOnInit(): void { forkJoin({ workflows: this.workflowService.getAll(), providers: this.providerService.getConnected() }).subscribe({ next: data => { this.workflows.set(data.workflows); this.connectedCount.set(data.providers.length); this.loading.set(false); }, error: () => { this.loading.set(false); this.toast.error('toast.loadError'); } }); }
create(): void { if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; } const value = this.createForm.getRawValue(); this.workflowService.create(value.name, value.description).subscribe({ next: workflow => { this.workflows.update(items => [workflow, ...items]); this.showCreate.set(false); this.createForm.reset(); this.toast.showSuccess('toast.workflowCreated'); void this.router.navigate(['/workflows', workflow.id]); }, error: () => this.toast.error('toast.saveError') }); }
delete(workflow: Workflow, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (!confirm(`${workflow.name}?`)) return;
    this.workflowService.delete(workflow.id).subscribe({
      next: () => {
        this.workflows.update(items => items.filter(item => item.id !== workflow.id));
        this.toast.showSuccess('toast.workflowDeleted');
      },
      error: () => this.toast.error('toast.deleteError'),
    });
  }
}
