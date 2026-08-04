import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Workflow } from '../../../core/models/workflow.model';
import { ToastService } from '../../../core/services/toast.service';
import { WorkflowService } from '../../../core/services/workflow.service';
@Component({ selector: 'app-workflow-list', imports: [RouterLink, TranslatePipe], templateUrl: './workflow-list.html', styleUrl: './workflow-list.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class WorkflowList implements OnInit { readonly workflows = signal<Workflow[]>([]); readonly loading = signal(true); constructor(private readonly service: WorkflowService, private readonly toast: ToastService) {} ngOnInit(): void { this.service.getAll().subscribe({ next: rows => { this.workflows.set(rows); this.loading.set(false); }, error: () => { this.loading.set(false); this.toast.error('toast.loadError'); } }); } delete(item: Workflow, event: Event): void { event.preventDefault(); if (!confirm(`${item.name}?`)) return; this.service.delete(item.id).subscribe({ next: () => this.workflows.update(rows => rows.filter(row => row.id !== item.id)), error: () => this.toast.error('toast.deleteError') }); } }
