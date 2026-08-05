import { ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, ViewChild, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ConnectedProvider } from '../../../core/models/provider.model';
import {
  NodeExecutionResult,
  Workflow,
  WorkflowConnection,
  WorkflowExecutionResult,
  WorkflowNode,
} from '../../../core/models/workflow.model';
import { ProviderService } from '../../../core/services/provider.service';
import { ToastService } from '../../../core/services/toast.service';
import { WorkflowService } from '../../../core/services/workflow.service';
import { ModelSelect } from '../../../shared/components/model-select/model-select';

interface DragState {
  id: number;
  offsetX: number;
  offsetY: number;
}

interface Point {
  x: number;
  y: number;
}

@Component({
  selector: 'app-workflow-editor',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ModelSelect],
  templateUrl: './workflow-editor.html',
  styleUrl: './workflow-editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowEditor implements OnInit {
  @ViewChild('canvas') private canvas?: ElementRef<HTMLElement>;

  readonly workflow = signal<Workflow | null>(null);
  readonly nodes = signal<WorkflowNode[]>([]);
  readonly connections = signal<WorkflowConnection[]>([]);
  readonly providers = signal<ConnectedProvider[]>([]);
  readonly selected = signal<WorkflowNode | null>(null);
  readonly sourceNodeId = signal<number | null>(null);
  readonly pointer = signal<Point | null>(null);
  readonly showAdd = signal(false);
  readonly showExecute = signal(false);
  readonly executing = signal(false);
  readonly results = signal<NodeExecutionResult[]>([]);
  readonly drag = signal<DragState | null>(null);
  readonly addProviderId = signal(0);
  readonly editProviderId = signal(0);

  readonly canvasWidth = computed(() => Math.max(1200, ...this.nodes().map((n) => n.positionX + 350), 0));
  readonly canvasHeight = computed(() => Math.max(700, ...this.nodes().map((n) => n.positionY + 280), 0));
  readonly viewBox = computed(() => `0 0 ${this.canvasWidth()} ${this.canvasHeight()}`);
  readonly isConnecting = computed(() => this.sourceNodeId() !== null);
  readonly previewPath = computed(() => {
    const sourceId = this.sourceNodeId();
    const pointer = this.pointer();
    if (sourceId === null || !pointer) {
      return '';
    }
    const source = this.nodes().find((n) => n.id === sourceId);
    if (!source) {
      return '';
    }
    return this.bezier(
      source.positionX + 220,
      source.positionY + 65,
      pointer.x,
      pointer.y,
    );
  });

  readonly editForm;
  readonly addForm;
  readonly executeForm;
  private workflowId = 0;

  constructor(
    route: ActivatedRoute,
    fb: FormBuilder,
    private readonly service: WorkflowService,
    private readonly providerService: ProviderService,
    private readonly toast: ToastService,
  ) {
    this.workflowId = Number(route.snapshot.paramMap.get('id'));
    this.editForm = fb.nonNullable.group({
      name: ['', Validators.required],
      model: ['', Validators.required],
      prompt: ['', Validators.required],
      inputMapping: [''],
      order: [0],
      userAIProviderId: [{ value: 0, disabled: true }],
    });
    this.addForm = fb.nonNullable.group({
      userAIProviderId: [0, [Validators.required, Validators.min(1)]],
      model: ['gpt-4o-mini', Validators.required],
      name: ['', Validators.required],
      prompt: ['', Validators.required],
    });
    this.executeForm = fb.nonNullable.group({
      input: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    if (!Number.isInteger(this.workflowId)) {
      return;
    }

    forkJoin({
      workflow: this.service.getById(this.workflowId),
      nodes: this.service.getNodes(this.workflowId),
      connections: this.service.getConnections(this.workflowId),
      providers: this.providerService.getConnected(),
    }).subscribe({
      next: (data) => {
        this.workflow.set(data.workflow);
        this.nodes.set(data.nodes.map((n) => this.normalizeNode(n)));
        this.connections.set(data.connections.map((c) => this.normalizeConnection(c)));
        this.providers.set(data.providers);
        if (data.providers[0]) {
          const preferred = data.providers.find((p) => p.isDefault) ?? data.providers[0];
          this.addProviderId.set(preferred.id);
          this.addForm.patchValue({
            userAIProviderId: preferred.id,
            model: preferred.defaultModel ?? 'gpt-4o-mini',
          });
        }
      },
      error: () => this.toast.error('toast.loadError'),
    });
  }

  providerById(id: number): ConnectedProvider | null {
    return this.providers().find((p) => p.id === id) ?? null;
  }

  onAddProviderChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
    const provider = this.providerById(id);
    this.addProviderId.set(id);
    this.addForm.patchValue({
      userAIProviderId: id,
      model: provider?.defaultModel ?? '',
    });
  }

  @HostListener('document:keydown.escape')
  cancelConnection(): void {
    this.sourceNodeId.set(null);
    this.pointer.set(null);
  }

  selectNode(node: WorkflowNode): void {
    if (this.isConnecting()) {
      return;
    }
    this.selected.set(node);
    this.editProviderId.set(node.userAIProviderId);
    this.editForm.reset({
      name: node.name,
      model: node.model,
      prompt: node.prompt,
      inputMapping: node.inputMapping ?? '',
      order: node.order,
      userAIProviderId: node.userAIProviderId,
    });
  }

  closePanel(): void {
    this.selected.set(null);
  }

  saveNode(): void {
    const node = this.selected();
    if (!node || this.editForm.invalid) {
      return;
    }

    const value = this.editForm.getRawValue();
    this.service
      .updateNode(node.id, {
        id: node.id,
        name: value.name,
        model: value.model,
        prompt: value.prompt,
        inputMapping: value.inputMapping,
        order: value.order,
        nodeType: node.nodeType,
      })
      .subscribe({
        next: () => {
          this.nodes.update((rows) => rows.map((row) => (row.id === node.id ? { ...row, ...value } : row)));
          this.selected.set({ ...node, ...value });
          this.toast.showSuccess('toast.nodeSaved');
        },
        error: () => this.toast.error('toast.saveError'),
      });
  }

  addNode(): void {
    if (!this.providers().length) {
      this.toast.warning('toast.connectProviderFirst');
      return;
    }
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const value = this.addForm.getRawValue();
    this.service
      .addNode({
        workflowId: this.workflowId,
        userAIProviderId: Number(value.userAIProviderId),
        model: value.model,
        name: value.name,
        prompt: value.prompt,
        inputMapping: '',
        order: this.nodes().length,
        positionX: 80 + (this.nodes().length % 3) * 280,
        positionY: 80 + Math.floor(this.nodes().length / 3) * 210,
        nodeType: 'AI',
      })
      .subscribe({
        next: (node) => {
          this.nodes.update((rows) => [...rows, this.normalizeNode(node)]);
          this.showAdd.set(false);
          this.addForm.patchValue({ name: '', prompt: '' });
          this.toast.showSuccess('toast.nodeAdded');
        },
        error: () => this.toast.error('toast.saveError'),
      });
  }

  deleteSelected(): void {
    const node = this.selected();
    if (!node || !confirm(node.name)) {
      return;
    }

    this.service.deleteNode(node.id).subscribe({
      next: () => {
        this.nodes.update((rows) => rows.filter((row) => row.id !== node.id));
        this.connections.update((rows) =>
          rows.filter((row) => row.sourceNodeId !== node.id && row.targetNodeId !== node.id),
        );
        this.selected.set(null);
        this.toast.showSuccess('toast.nodeDeleted');
      },
      error: () => this.toast.error('toast.deleteError'),
    });
  }

  startConnection(nodeId: number, event: PointerEvent): void {
    event.stopPropagation();
    event.preventDefault();
    this.selected.set(null);
    this.sourceNodeId.set(nodeId);
    this.setPointerFromEvent(event);
  }

  finishConnection(targetNodeId: number, event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    const sourceNodeId = this.sourceNodeId();
    if (
      !sourceNodeId ||
      sourceNodeId === targetNodeId ||
      this.connections().some((c) => c.sourceNodeId === sourceNodeId && c.targetNodeId === targetNodeId)
    ) {
      this.cancelConnection();
      return;
    }

    this.service.addConnection({ workflowId: this.workflowId, sourceNodeId, targetNodeId }).subscribe({
      next: (connection) => {
        this.connections.update((rows) => [...rows, this.normalizeConnection(connection)]);
        this.cancelConnection();
      },
      error: () => {
        this.toast.error('toast.connectionError');
        this.cancelConnection();
      },
    });
  }

  onCanvasPointerMove(event: PointerEvent): void {
    this.moveDrag(event);
    if (this.sourceNodeId() !== null) {
      this.setPointerFromEvent(event);
    }
  }

  onCanvasPointerDown(event: PointerEvent): void {
    if (!this.isConnecting()) {
      return;
    }
    const target = event.target as HTMLElement;
    if (target.classList.contains('port') || target.closest('.node-card')) {
      return;
    }
    this.cancelConnection();
  }

  startDrag(node: WorkflowNode, event: PointerEvent): void {
    const target = event.target as HTMLElement;
    if (target.classList.contains('port') || target.closest('.port') || this.isConnecting()) {
      return;
    }
    const canvasEl = this.canvas?.nativeElement;
    if (!canvasEl) {
      return;
    }
    const rect = canvasEl.getBoundingClientRect();
    this.drag.set({
      id: node.id,
      offsetX: event.clientX - rect.left + canvasEl.scrollLeft - node.positionX,
      offsetY: event.clientY - rect.top + canvasEl.scrollTop - node.positionY,
    });
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  moveDrag(event: PointerEvent): void {
    const drag = this.drag();
    const canvasEl = this.canvas?.nativeElement;
    if (!drag || !canvasEl) {
      return;
    }
    const rect = canvasEl.getBoundingClientRect();
    const positionX = Math.max(20, event.clientX - rect.left + canvasEl.scrollLeft - drag.offsetX);
    const positionY = Math.max(20, event.clientY - rect.top + canvasEl.scrollTop - drag.offsetY);
    this.nodes.update((rows) => rows.map((row) => (row.id === drag.id ? { ...row, positionX, positionY } : row)));
  }

  endDrag(): void {
    const drag = this.drag();
    if (!drag) {
      return;
    }
    this.drag.set(null);
    const node = this.nodes().find((row) => row.id === drag.id);
    if (node) {
      this.service
        .moveNode(node.id, { nodeId: node.id, positionX: node.positionX, positionY: node.positionY })
        .subscribe({ error: () => this.toast.error('toast.moveError') });
    }
  }

  path(connection: WorkflowConnection): string {
    const source = this.nodes().find((n) => n.id === connection.sourceNodeId);
    const target = this.nodes().find((n) => n.id === connection.targetNodeId);
    if (!source || !target) {
      return '';
    }
    return this.bezier(
      source.positionX + 220,
      source.positionY + 65,
      target.positionX,
      target.positionY + 65,
    );
  }

  execute(): void {
    if (this.executeForm.invalid) {
      return;
    }
    this.executing.set(true);
    this.results.set([]);
    this.service.execute(this.workflowId, this.executeForm.controls.input.value).subscribe({
      next: (result) => {
        this.results.set(this.normalizeResults(result));
        this.executing.set(false);
      },
      error: () => {
        this.executing.set(false);
        this.toast.error('toast.executeError');
      },
    });
  }

  private bezier(x1: number, y1: number, x2: number, y2: number): string {
    const curve = Math.max(70, Math.abs(x2 - x1) * 0.45);
    return `M ${x1} ${y1} C ${x1 + curve} ${y1}, ${x2 - curve} ${y2}, ${x2} ${y2}`;
  }

  private setPointerFromEvent(event: PointerEvent): void {
    const canvasEl = this.canvas?.nativeElement;
    if (!canvasEl) {
      return;
    }
    const rect = canvasEl.getBoundingClientRect();
    this.pointer.set({
      x: event.clientX - rect.left + canvasEl.scrollLeft,
      y: event.clientY - rect.top + canvasEl.scrollTop,
    });
  }

  private normalizeNode(node: WorkflowNode | Record<string, unknown>): WorkflowNode {
    const value = node as Record<string, unknown>;
    return {
      id: Number(value['id'] ?? value['Id']),
      workflowId: Number(value['workflowId'] ?? value['WorkflowId']),
      userAIProviderId: Number(value['userAIProviderId'] ?? value['UserAIProviderId']),
      model: String(value['model'] ?? value['Model'] ?? ''),
      name: String(value['name'] ?? value['Name'] ?? ''),
      prompt: String(value['prompt'] ?? value['Prompt'] ?? ''),
      inputMapping: (value['inputMapping'] ?? value['InputMapping'] ?? null) as string | null,
      order: Number(value['order'] ?? value['Order'] ?? 0),
      positionX: Number(value['positionX'] ?? value['PositionX'] ?? 0),
      positionY: Number(value['positionY'] ?? value['PositionY'] ?? 0),
      nodeType: String(value['nodeType'] ?? value['NodeType'] ?? 'AI'),
    };
  }

  private normalizeConnection(connection: WorkflowConnection | Record<string, unknown>): WorkflowConnection {
    const value = connection as Record<string, unknown>;
    return {
      id: Number(value['id'] ?? value['Id']),
      workflowId: Number(value['workflowId'] ?? value['WorkflowId']),
      sourceNodeId: Number(value['sourceNodeId'] ?? value['SourceNodeId']),
      targetNodeId: Number(value['targetNodeId'] ?? value['TargetNodeId']),
      sourceOutput: (value['sourceOutput'] ?? value['SourceOutput']) as string | undefined,
      targetInput: (value['targetInput'] ?? value['TargetInput']) as string | undefined,
    };
  }

  private normalizeResults(result: WorkflowExecutionResult): NodeExecutionResult[] {
    const raw = result.nodeResults;
    if (!raw) {
      return [];
    }
    if (Array.isArray(raw)) {
      return raw;
    }
    return Object.entries(raw).map(([key, value]) => ({
      ...value,
      nodeId: value.nodeId ?? Number(key),
    }));
  }
}
