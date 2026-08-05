import { ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, ViewChild, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
import { FieldError } from '../../../shared/components/field-error/field-error';
import { AppSelect, AppSelectOption } from '../../../shared/components/app-select/app-select';

interface DragState {
  id: number;
  offsetX: number;
  offsetY: number;
}

interface Point {
  x: number;
  y: number;
}

interface PanState {
  startX: number;
  startY: number;
  originPanX: number;
  originPanY: number;
}

type PendingDelete =
  | { kind: 'node'; node: WorkflowNode }
  | { kind: 'connection'; connection: WorkflowConnection };

interface PendingBranchConnection {
  sourceNodeId: number;
  targetNodeId: number;
  options: string[];
}

const MIN_ZOOM = 0.25;
const MAX_ZOOM = 2.5;

@Component({
  selector: 'app-workflow-editor',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ModelSelect, FieldError, AppSelect],
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
  readonly pendingDelete = signal<PendingDelete | null>(null);
  readonly pendingBranch = signal<PendingBranchConnection | null>(null);
  readonly drag = signal<DragState | null>(null);
  readonly panSession = signal<PanState | null>(null);
  readonly zoom = signal(1);
  readonly panX = signal(0);
  readonly panY = signal(0);
  readonly addProviderId = signal(0);
  readonly editProviderId = signal(0);
  readonly addNodeType = signal('AI');

  readonly canvasWidth = computed(() => Math.max(1200, ...this.nodes().map((n) => n.positionX + 350), 0));
  readonly canvasHeight = computed(() => Math.max(700, ...this.nodes().map((n) => n.positionY + 280), 0));
  readonly viewBox = computed(() => `0 0 ${this.canvasWidth()} ${this.canvasHeight()}`);
  readonly isConnecting = computed(() => this.sourceNodeId() !== null);
  readonly isPanning = computed(() => this.panSession() !== null);
  readonly zoomPercent = computed(() => Math.round(this.zoom() * 100));
  readonly worldTransform = computed(
    () => `translate(${this.panX()}px, ${this.panY()}px) scale(${this.zoom()})`,
  );
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
    private readonly router: Router,
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
      nodeType: ['AI', Validators.required],
      userAIProviderId: [0],
      model: ['gpt-4o-mini'],
      name: ['', Validators.required],
      prompt: [''],
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

  readonly nodeTypeOptions: AppSelectOption[] = [
    { value: 'AI', label: 'AI' },
    { value: 'If', label: 'If / Else' },
    { value: 'Parallel', label: 'Parallel' },
    { value: 'Join', label: 'Join' },
    { value: 'Loop', label: 'Loop' },
  ];

  readonly providerOptions = computed<AppSelectOption[]>(() =>
    this.providers().map((provider) => ({
      value: String(provider.id),
      label: provider.providerName,
    })),
  );

  onAddProviderChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
    this.applyAddProvider(id);
  }

  onAddProviderSelect(value: string): void {
    this.applyAddProvider(Number(value) || 0);
  }

  onEditProviderSelect(value: string): void {
    const id = Number(value) || 0;
    const provider = this.providerById(id);
    this.editProviderId.set(id);
    this.editForm.patchValue({
      userAIProviderId: id,
      model: provider?.defaultModel ?? this.editForm.controls.model.value,
    });
  }

  onAddNodeTypeSelect(value: string): void {
    this.addNodeType.set(value);
    this.addForm.controls.nodeType.setValue(value);
  }

  private applyAddProvider(id: number): void {
    const provider = this.providerById(id);
    this.addProviderId.set(id);
    this.addForm.patchValue({
      userAIProviderId: id,
      model: provider?.defaultModel ?? '',
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.pendingDelete()) {
      this.cancelPendingDelete();
      return;
    }
    if (this.pendingBranch()) {
      this.cancelPendingBranch();
      return;
    }
    this.cancelConnection();
  }

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
    this.syncEditValidators(node.nodeType);
    this.editForm.reset({
      name: node.name,
      model: node.model,
      prompt: node.prompt,
      inputMapping: node.inputMapping ?? '',
      order: node.order,
      userAIProviderId: node.userAIProviderId,
    });
  }

  private syncEditValidators(nodeType: string): void {
    const isAi = nodeType === 'AI';
    const needsPrompt = isAi || nodeType === 'If' || nodeType === 'Loop';
    this.editForm.controls.model.setValidators(isAi ? [Validators.required] : []);
    this.editForm.controls.prompt.setValidators(needsPrompt ? [Validators.required] : []);
    this.editForm.controls.model.updateValueAndValidity({ emitEvent: false });
    this.editForm.controls.prompt.updateValueAndValidity({ emitEvent: false });
  }

  closePanel(): void {
    this.selected.set(null);
  }

  saveNode(): void {
    const node = this.selected();
    if (!node) {
      return;
    }
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
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

    const value = this.addForm.getRawValue();
    const nodeType = value.nodeType || 'AI';
    const isAi = nodeType === 'AI';
    const needsPrompt = isAi || nodeType === 'If' || nodeType === 'Loop';

    this.addForm.controls.model.setValidators(isAi ? [Validators.required] : []);
    this.addForm.controls.prompt.setValidators(needsPrompt ? [Validators.required] : []);
    this.addForm.controls.userAIProviderId.setValidators(isAi ? [Validators.required, Validators.min(1)] : []);
    this.addForm.controls.model.updateValueAndValidity({ emitEvent: false });
    this.addForm.controls.prompt.updateValueAndValidity({ emitEvent: false });
    this.addForm.controls.userAIProviderId.updateValueAndValidity({ emitEvent: false });

    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    this.service
      .addNode({
        workflowId: this.workflowId,
        userAIProviderId: Number(value.userAIProviderId) || this.addProviderId() || 0,
        model: isAi ? value.model : '-',
        name: value.name.trim(),
        prompt: value.prompt ?? '',
        inputMapping: '',
        order: this.nodes().length,
        positionX: 80 + (this.nodes().length % 3) * 280,
        positionY: 80 + Math.floor(this.nodes().length / 3) * 210,
        nodeType,
      })
      .subscribe({
        next: (node) => {
          this.nodes.update((rows) => [...rows, this.normalizeNode(node)]);
          this.showAdd.set(false);
          this.addForm.patchValue({ name: '', prompt: '', nodeType: 'AI' });
          this.addNodeType.set('AI');
          this.toast.showSuccess('toast.nodeAdded');
        },
        error: () => this.toast.error('toast.saveError'),
      });
  }

  deleteSelected(): void {
    const node = this.selected();
    if (!node) {
      return;
    }
    this.requestDeleteNode(node);
  }

  deleteNode(node: WorkflowNode, event?: Event): void {
    event?.stopPropagation();
    event?.preventDefault();
    this.requestDeleteNode(node);
  }

  requestDeleteNode(node: WorkflowNode): void {
    this.pendingDelete.set({ kind: 'node', node });
  }

  requestDeleteConnection(connection: WorkflowConnection, event?: Event): void {
    event?.stopPropagation();
    event?.preventDefault();
    if (this.isConnecting()) {
      return;
    }
    this.selected.set(null);
    this.pendingDelete.set({ kind: 'connection', connection });
  }

  cancelPendingDelete(): void {
    this.pendingDelete.set(null);
  }

  confirmPendingDelete(): void {
    const pending = this.pendingDelete();
    if (!pending) {
      return;
    }

    if (pending.kind === 'node') {
      const node = pending.node;
      this.service.deleteNode(node.id).subscribe({
        next: () => {
          this.nodes.update((rows) => rows.filter((row) => row.id !== node.id));
          this.connections.update((rows) =>
            rows.filter((row) => row.sourceNodeId !== node.id && row.targetNodeId !== node.id),
          );
          if (this.selected()?.id === node.id) {
            this.selected.set(null);
          }
          if (this.sourceNodeId() === node.id) {
            this.cancelConnection();
          }
          this.pendingDelete.set(null);
          this.toast.showSuccess('toast.nodeDeleted');
        },
        error: () => this.toast.error('toast.deleteError'),
      });
      return;
    }

    const connection = pending.connection;
    this.service.deleteConnection(connection.id).subscribe({
      next: () => {
        this.connections.update((rows) => rows.filter((row) => row.id !== connection.id));
        this.pendingDelete.set(null);
        this.toast.showSuccess('toast.connectionDeleted');
      },
      error: () => this.toast.error('toast.deleteError'),
    });
  }

  nodeName(id: number): string {
    return this.nodes().find((node) => node.id === id)?.name ?? `#${id}`;
  }

  isPendingConnection(connectionId: number): boolean {
    const pending = this.pendingDelete();
    return pending?.kind === 'connection' && pending.connection.id === connectionId;
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

    const source = this.nodes().find((n) => n.id === sourceNodeId);
    if (source?.nodeType === 'If') {
      this.pendingBranch.set({
        sourceNodeId,
        targetNodeId,
        options: ['true', 'false', 'default'],
      });
      this.cancelConnection();
      return;
    }
    if (source?.nodeType === 'Loop') {
      this.pendingBranch.set({
        sourceNodeId,
        targetNodeId,
        options: ['body', 'done'],
      });
      this.cancelConnection();
      return;
    }

    this.createConnection(sourceNodeId, targetNodeId, null);
  }

  cancelPendingBranch(): void {
    this.pendingBranch.set(null);
  }

  selectBranchKey(conditionKey: string): void {
    const pending = this.pendingBranch();
    if (!pending) {
      return;
    }
    this.pendingBranch.set(null);
    this.createConnection(pending.sourceNodeId, pending.targetNodeId, conditionKey);
  }

  private createConnection(sourceNodeId: number, targetNodeId: number, conditionKey: string | null): void {
    this.service
      .addConnection({
        workflowId: this.workflowId,
        sourceNodeId,
        targetNodeId,
        conditionKey,
        priority: this.connections().filter((c) => c.sourceNodeId === sourceNodeId).length,
      })
      .subscribe({
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
    const pan = this.panSession();
    if (pan) {
      this.panX.set(pan.originPanX + (event.clientX - pan.startX));
      this.panY.set(pan.originPanY + (event.clientY - pan.startY));
      return;
    }
    this.moveDrag(event);
    if (this.sourceNodeId() !== null) {
      this.setPointerFromEvent(event);
    }
  }

  onCanvasPointerDown(event: PointerEvent): void {
    const target = event.target as HTMLElement;
    const onNode = Boolean(target.closest('.node-card') || target.closest('.port'));
    const onEdge = Boolean(target.closest('.edge-hit'));

    if (this.isConnecting()) {
      if (!onNode && !onEdge) {
        this.cancelConnection();
      }
      return;
    }

    const middle = event.button === 1;
    const emptyLeft = event.button === 0 && !onNode && !onEdge;
    if (!middle && !emptyLeft) {
      return;
    }

    event.preventDefault();
    this.panSession.set({
      startX: event.clientX,
      startY: event.clientY,
      originPanX: this.panX(),
      originPanY: this.panY(),
    });
    this.canvas?.nativeElement.setPointerCapture(event.pointerId);
  }

  onCanvasWheel(event: WheelEvent): void {
    event.preventDefault();
    const factor = event.deltaY < 0 ? 1.08 : 1 / 1.08;
    this.zoomAtClient(event.clientX, event.clientY, factor);
  }

  zoomIn(): void {
    this.zoomAtCenter(1.15);
  }

  zoomOut(): void {
    this.zoomAtCenter(1 / 1.15);
  }

  resetView(): void {
    this.zoom.set(1);
    this.panX.set(0);
    this.panY.set(0);
  }

  fitToView(): void {
    const canvasEl = this.canvas?.nativeElement;
    const nodes = this.nodes();
    if (!canvasEl) {
      return;
    }
    if (!nodes.length) {
      this.resetView();
      return;
    }

    const minX = Math.min(...nodes.map((n) => n.positionX));
    const minY = Math.min(...nodes.map((n) => n.positionY));
    const maxX = Math.max(...nodes.map((n) => n.positionX + 240));
    const maxY = Math.max(...nodes.map((n) => n.positionY + 160));
    const worldW = Math.max(1, maxX - minX);
    const worldH = Math.max(1, maxY - minY);
    const rect = canvasEl.getBoundingClientRect();
    const pad = 96;
    const nextZoom = this.clampZoom(
      Math.min((rect.width - pad) / worldW, (rect.height - pad) / worldH),
    );
    this.zoom.set(nextZoom);
    this.panX.set((rect.width - worldW * nextZoom) / 2 - minX * nextZoom);
    this.panY.set((rect.height - worldH * nextZoom) / 2 - minY * nextZoom);
  }

  startDrag(node: WorkflowNode, event: PointerEvent): void {
    const target = event.target as HTMLElement;
    if (
      target.classList.contains('port') ||
      target.closest('.port') ||
      target.closest('.node-delete') ||
      this.isConnecting() ||
      event.button !== 0
    ) {
      return;
    }
    event.stopPropagation();
    const world = this.screenToWorld(event);
    this.drag.set({
      id: node.id,
      offsetX: world.x - node.positionX,
      offsetY: world.y - node.positionY,
    });
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  moveDrag(event: PointerEvent): void {
    const drag = this.drag();
    if (!drag) {
      return;
    }
    const world = this.screenToWorld(event);
    const positionX = Math.max(20, world.x - drag.offsetX);
    const positionY = Math.max(20, world.y - drag.offsetY);
    this.nodes.update((rows) => rows.map((row) => (row.id === drag.id ? { ...row, positionX, positionY } : row)));
  }

  endPointer(): void {
    this.panSession.set(null);
    this.endDrag();
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

  labelX(connection: WorkflowConnection): number {
    const source = this.nodes().find((n) => n.id === connection.sourceNodeId);
    const target = this.nodes().find((n) => n.id === connection.targetNodeId);
    if (!source || !target) {
      return 0;
    }
    return (source.positionX + 220 + target.positionX) / 2;
  }

  labelY(connection: WorkflowConnection): number {
    const source = this.nodes().find((n) => n.id === connection.sourceNodeId);
    const target = this.nodes().find((n) => n.id === connection.targetNodeId);
    if (!source || !target) {
      return 0;
    }
    return (source.positionY + target.positionY) / 2 + 50;
  }

  goToChat(): void {
    void this.router.navigate(['/chat'], {
      queryParams: { workflowId: this.workflowId },
    });
  }

  execute(): void {
    if (this.executeForm.invalid) {
      this.executeForm.markAllAsTouched();
      return;
    }
    this.showExecute.set(false);
    void this.router.navigate(['/chat'], {
      queryParams: { workflowId: this.workflowId },
    });
  }

  private bezier(x1: number, y1: number, x2: number, y2: number): string {
    const curve = Math.max(70, Math.abs(x2 - x1) * 0.45);
    return `M ${x1} ${y1} C ${x1 + curve} ${y1}, ${x2 - curve} ${y2}, ${x2} ${y2}`;
  }

  private setPointerFromEvent(event: PointerEvent): void {
    this.pointer.set(this.screenToWorld(event));
  }

  private screenToWorld(event: { clientX: number; clientY: number }): Point {
    const canvasEl = this.canvas?.nativeElement;
    if (!canvasEl) {
      return { x: 0, y: 0 };
    }
    const rect = canvasEl.getBoundingClientRect();
    const zoom = this.zoom() || 1;
    return {
      x: (event.clientX - rect.left - this.panX()) / zoom,
      y: (event.clientY - rect.top - this.panY()) / zoom,
    };
  }

  private zoomAtCenter(factor: number): void {
    const canvasEl = this.canvas?.nativeElement;
    if (!canvasEl) {
      return;
    }
    const rect = canvasEl.getBoundingClientRect();
    this.zoomAtClient(rect.left + rect.width / 2, rect.top + rect.height / 2, factor);
  }

  private zoomAtClient(clientX: number, clientY: number, factor: number): void {
    const canvasEl = this.canvas?.nativeElement;
    if (!canvasEl) {
      return;
    }
    const rect = canvasEl.getBoundingClientRect();
    const mx = clientX - rect.left;
    const my = clientY - rect.top;
    const prev = this.zoom();
    const next = this.clampZoom(prev * factor);
    if (next === prev) {
      return;
    }
    const worldX = (mx - this.panX()) / prev;
    const worldY = (my - this.panY()) / prev;
    this.zoom.set(next);
    this.panX.set(mx - worldX * next);
    this.panY.set(my - worldY * next);
  }

  private clampZoom(value: number): number {
    return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, value));
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
      conditionKey: (value['conditionKey'] ?? value['ConditionKey'] ?? null) as string | null,
      priority: Number(value['priority'] ?? value['Priority'] ?? 0),
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
