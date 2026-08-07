import {
  ApplicationRef,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subscription, forkJoin } from 'rxjs';
import {
  AttachmentItem,
  ChatStreamEvent,
  ChatUiMessage,
  ConversationFolder,
  ConversationListItem,
} from '../../core/models/chat.model';
import { ConnectedProvider } from '../../core/models/provider.model';
import { Workflow } from '../../core/models/workflow.model';
import { ChatService } from '../../core/services/chat.service';
import { AppDialogService } from '../../core/services/app-dialog.service';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { WorkflowService } from '../../core/services/workflow.service';
import { FieldError } from '../../shared/components/field-error/field-error';
import { AppSelect, AppSelectOption } from '../../shared/components/app-select/app-select';

@Component({
  selector: 'app-chat',
  imports: [NgTemplateOutlet, ReactiveFormsModule, TranslatePipe, FieldError, AppSelect],
  templateUrl: './chat.html',
  styleUrl: './chat.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Chat implements OnInit, OnDestroy {
  @ViewChild('messageList') private messageList?: ElementRef<HTMLElement>;
  @ViewChild('composerInput') private composerInput?: ElementRef<HTMLTextAreaElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly chatService = inject(ChatService);
  private readonly providerService = inject(ProviderService);
  private readonly workflowService = inject(WorkflowService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly dialogs = inject(AppDialogService);
  private readonly fb = inject(FormBuilder);
  private readonly appRef = inject(ApplicationRef);

  readonly conversations = signal<ConversationListItem[]>([]);
  readonly folders = signal<ConversationFolder[]>([]);
  readonly messages = signal<ChatUiMessage[]>([]);
  readonly pendingAttachments = signal<AttachmentItem[]>([]);
  readonly providers = signal<ConnectedProvider[]>([]);
  readonly workflows = signal<Workflow[]>([]);
  readonly conversationId = signal<number | null>(null);
  readonly conversationTitle = signal('');
  readonly loading = signal(false);
  readonly sending = signal(false);
  readonly historyOpen = signal(false);
  readonly renamingId = signal<number | null>(null);
  readonly menuOpenId = signal<number | null>(null);
  readonly folderSubmenuOpen = signal(false);
  readonly dragConversationId = signal<number | null>(null);
  readonly dropTargetKey = signal<string | null>(null);
  readonly selectedProviderId = signal(0);
  readonly workflowMode = signal(false);
  readonly uploadingAttachments = signal(0);
  readonly activeNodeName = signal<string | null>(null);
  readonly activeNodeModel = signal<string | null>(null);
  readonly streamingContent = signal('');
  readonly thinking = signal(false);
  readonly selectedWorkflowId = signal(0);

  readonly processingLabel = computed(() => {
    const name = this.activeNodeName();
    if (!name) {
      return null;
    }
    const model = this.activeNodeModel();
    return model ? `${name} · ${model}` : name;
  });

  readonly composerForm = this.fb.nonNullable.group({
    message: ['', Validators.required],
    workflowId: [0],
    userProviderId: [0, [Validators.required, Validators.min(1)]],
    model: ['', Validators.required],
  });

  readonly selectedProvider = computed(() => {
    const id = this.selectedProviderId();
    return this.providers().find((p) => p.id === id) ?? null;
  });

  readonly workflowOptions = computed<AppSelectOption[]>(() => [
    {
      value: '0',
      label: this.translate.instant('chat.defaultModel'),
    },
    ...this.workflows().map((workflow) => ({
      value: String(workflow.id),
      label: workflow.name,
    })),
  ]);

  private streamSub?: Subscription;
  private pendingWorkflowId: number | null = null;
  private syncingWorkflowFromRoute = false;

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      const raw = params.get('workflowId');
      const id = raw ? Number(raw) : 0;
      this.pendingWorkflowId = Number.isFinite(id) && id > 0 ? id : null;
      if (!this.pendingWorkflowId) {
        return;
      }
      this.syncingWorkflowFromRoute = true;
      this.tryApplyPendingWorkflow();
      this.syncingWorkflowFromRoute = false;
    });

    forkJoin({
      conversations: this.chatService.getConversations(),
      folders: this.chatService.getFolders(),
      providers: this.providerService.getConnected(),
      workflows: this.workflowService.getAll(),
    }).subscribe({
      next: (data) => {
        this.conversations.set(data.conversations);
        this.folders.set(data.folders);
        this.providers.set(data.providers);
        this.workflows.set(data.workflows.filter((w) => w.isActive !== false));

        const preferred =
          data.providers.find((p) => p.isDefault) ?? data.providers[0] ?? null;
        if (preferred) {
          this.selectedProviderId.set(preferred.id);
          this.composerForm.patchValue({
            userProviderId: preferred.id,
            model: preferred.defaultModel ?? '',
          });
        }

        this.applyWorkflowSelection(this.selectedWorkflowId() || this.pendingWorkflowId || 0, {
          syncUrl: false,
        });
        this.tryApplyPendingWorkflow();
      },
      error: () => this.toast.error('toast.loadError'),
    });

    this.route.paramMap.subscribe((params) => {
      const raw = params.get('conversationId');
      if (!raw) {
        this.conversationId.set(null);
        this.conversationTitle.set('');
        this.messages.set([]);
        queueMicrotask(() => this.focusComposer());
        return;
      }

      const id = Number(raw);
      if (!Number.isFinite(id) || id <= 0) {
        void this.router.navigate(['/chat']);
        return;
      }

      this.loadConversation(id);
    });
  }

  ngOnDestroy(): void {
    this.streamSub?.unsubscribe();
  }

  providerIdValue(): number {
    return this.selectedProviderId();
  }

  onWorkflowSelect(value: string): void {
    this.applyWorkflowSelection(Number(value) || 0, { syncUrl: true });
  }

  newChat(): void {
    this.historyOpen.set(false);
    const workflowId = this.selectedWorkflowId();
    void this.router.navigate(['/chat'], {
      queryParams: workflowId > 0 ? { workflowId } : {},
    });
  }

  openConversation(id: number): void {
    this.historyOpen.set(false);
    const workflowId = this.selectedWorkflowId();
    void this.router.navigate(['/chat', id], {
      queryParams: workflowId > 0 ? { workflowId } : {},
    });
  }

  conversationsForFolder(folderId: number | null): ConversationListItem[] {
    return this.conversations().filter((item) => (item.folderId ?? null) === folderId);
  }

  async createFolder(): Promise<void> {
    const name = await this.dialogs.prompt({
      title: this.translate.instant('chat.newFolder'),
      placeholder: this.translate.instant('chat.folderNamePlaceholder'),
      confirmLabel: this.translate.instant('common.create'),
    });
    if (!name) {
      return;
    }

    this.chatService.createFolder(name).subscribe({
      next: (folder) => this.folders.update((folders) => [...folders, folder]),
      error: () => this.toast.error('toast.folderError'),
    });
  }

  async renameFolder(folder: ConversationFolder, event: Event): Promise<void> {
    event.stopPropagation();
    const name = await this.dialogs.prompt({
      title: this.translate.instant('chat.renameFolder'),
      defaultValue: folder.name,
      placeholder: this.translate.instant('chat.folderNamePlaceholder'),
      confirmLabel: this.translate.instant('common.save'),
    });
    if (!name || name === folder.name) {
      return;
    }

    this.chatService.renameFolder(folder.id, name).subscribe({
      next: (updated) =>
        this.folders.update((folders) =>
          folders.map((item) => (item.id === updated.id ? updated : item)),
        ),
      error: () => this.toast.error('toast.folderError'),
    });
  }

  async deleteFolder(folder: ConversationFolder, event: Event): Promise<void> {
    event.stopPropagation();
    const ok = await this.dialogs.confirm({
      title: this.translate.instant('chat.deleteFolder'),
      message: this.translate.instant('chat.deleteFolderText', { name: folder.name }),
    });
    if (!ok) {
      return;
    }

    this.chatService.deleteFolder(folder.id).subscribe({
      next: () => {
        this.folders.update((folders) => folders.filter((item) => item.id !== folder.id));
        this.conversations.update((items) =>
          items.map((item) =>
            item.folderId === folder.id ? { ...item, folderId: null } : item,
          ),
        );
      },
      error: () => this.toast.error('toast.folderError'),
    });
  }

  togglePinned(item: ConversationListItem, event: Event): void {
    event.stopPropagation();
    this.chatService.setPinned(item.id, !item.isPinned).subscribe({
      next: (updated) => this.replaceAndSortConversation(updated),
      error: () => this.toast.error('toast.saveError'),
    });
  }

  moveToFolder(item: ConversationListItem, event: Event): void {
    event.stopPropagation();
    const raw = (event.target as HTMLSelectElement).value;
    const folderId = raw ? Number(raw) : null;
    this.assignConversationFolder(item, folderId);
  }

  toggleMenu(item: ConversationListItem, event: Event): void {
    event.stopPropagation();
    if (this.menuOpenId() === item.id) {
      this.closeMenu();
      return;
    }
    this.menuOpenId.set(item.id);
    this.folderSubmenuOpen.set(false);
  }

  toggleFolderSubmenu(event: Event): void {
    event.stopPropagation();
    this.folderSubmenuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpenId.set(null);
    this.folderSubmenuOpen.set(false);
  }

  onMenuRename(item: ConversationListItem, event: Event): void {
    this.closeMenu();
    this.startRename(item, event);
  }

  onMenuDelete(item: ConversationListItem, event: Event): void {
    this.closeMenu();
    void this.deleteConversation(item, event);
  }

  assignToFolder(item: ConversationListItem, folderId: number | null, event: Event): void {
    event.stopPropagation();
    this.closeMenu();
    this.assignConversationFolder(item, folderId);
  }

  onConversationDragStart(item: ConversationListItem, event: DragEvent): void {
    if (this.renamingId() === item.id) {
      event.preventDefault();
      return;
    }
    this.closeMenu();
    this.dragConversationId.set(item.id);
    event.dataTransfer?.setData('text/plain', String(item.id));
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  onConversationDragEnd(): void {
    this.dragConversationId.set(null);
    this.dropTargetKey.set(null);
  }

  onFolderDragOver(folderId: number | null, event: DragEvent): void {
    if (this.dragConversationId() === null) {
      return;
    }
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    this.dropTargetKey.set(this.dropKey(folderId));
  }

  onFolderDragLeave(folderId: number | null, event: DragEvent): void {
    const related = event.relatedTarget as Node | null;
    const current = event.currentTarget as HTMLElement;
    if (related && current.contains(related)) {
      return;
    }
    if (this.dropTargetKey() === this.dropKey(folderId)) {
      this.dropTargetKey.set(null);
    }
  }

  onFolderDrop(folderId: number | null, event: DragEvent): void {
    event.preventDefault();
    const raw = event.dataTransfer?.getData('text/plain');
    const conversationId = Number(raw || this.dragConversationId());
    this.dragConversationId.set(null);
    this.dropTargetKey.set(null);
    if (!Number.isFinite(conversationId) || conversationId <= 0) {
      return;
    }
    const item = this.conversations().find((row) => row.id === conversationId);
    if (!item) {
      return;
    }
    this.assignConversationFolder(item, folderId);
  }

  isDropTarget(folderId: number | null): boolean {
    return this.dropTargetKey() === this.dropKey(folderId);
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeMenu();
  }

  private dropKey(folderId: number | null): string {
    return folderId === null ? 'ungrouped' : `folder:${folderId}`;
  }

  private assignConversationFolder(item: ConversationListItem, folderId: number | null): void {
    if ((item.folderId ?? null) === folderId) {
      return;
    }
    this.chatService.assignFolder(item.id, folderId).subscribe({
      next: (updated) => this.replaceAndSortConversation(updated),
      error: () => this.toast.error('toast.folderError'),
    });
  }

  startRename(item: ConversationListItem, event: Event): void {
    event.stopPropagation();
    this.renamingId.set(item.id);
  }

  commitRename(item: ConversationListItem, event: Event): void {
    const input = event.target as HTMLInputElement;
    const title = input.value.trim();
    this.renamingId.set(null);

    if (!title || title === item.title) {
      return;
    }

    this.chatService.rename(item.id, title).subscribe({
      next: (updated) => {
        this.conversations.update((list) =>
          list.map((c) => (c.id === updated.id ? updated : c)),
        );
        if (this.conversationId() === updated.id) {
          this.conversationTitle.set(updated.title);
        }
      },
      error: () => this.toast.error('toast.saveError'),
    });
  }

  async deleteConversation(item: ConversationListItem, event: Event): Promise<void> {
    event.stopPropagation();
    const ok = await this.dialogs.confirm({
      title: this.translate.instant('chat.deleteConversation'),
      message: this.translate.instant('chat.deleteConversationText', { name: item.title }),
    });
    if (!ok) {
      return;
    }

    this.chatService.delete(item.id).subscribe({
      next: () => {
        this.conversations.update((list) => list.filter((c) => c.id !== item.id));
        if (this.conversationId() === item.id) {
          void this.router.navigate(['/chat']);
        }
      },
      error: () => this.toast.error('toast.deleteError'),
    });
  }

  onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  onAttachmentSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    for (const file of files) {
      this.uploadingAttachments.update((count) => count + 1);
      this.chatService.uploadAttachment(file).subscribe({
        next: (attachment) => {
          this.pendingAttachments.update((items) => [...items, attachment]);
          this.uploadingAttachments.update((count) => count - 1);
        },
        error: () => {
          this.uploadingAttachments.update((count) => count - 1);
          this.toast.error('toast.uploadError');
        },
      });
    }
  }

  removeAttachment(id: number): void {
    this.pendingAttachments.update((items) => items.filter((item) => item.id !== id));
  }

  send(): void {
    if (this.sending() || this.uploadingAttachments() > 0) {
      return;
    }

    const workflowId = this.selectedWorkflowId() || Number(this.composerForm.controls.workflowId.value) || 0;
    const message = this.composerForm.controls.message.value.trim();
    if (!message) {
      this.composerForm.controls.message.markAsTouched();
      return;
    }

    if (workflowId <= 0) {
      const provider = this.selectedProvider();
      const model =
        this.composerForm.controls.model.value || provider?.defaultModel || '';
      if (!provider || !model) {
        this.toast.error('toast.connectProviderFirst');
        return;
      }
    }

    const provider = this.selectedProvider();
    const model =
      this.composerForm.controls.model.value || provider?.defaultModel || '';

    const tempUserId = `local-user-${Date.now()}`;
    const attachments = this.pendingAttachments();
    this.messages.update((list) => [
      ...list,
      {
        id: tempUserId,
        role: 'user',
        content: message,
        createdAt: new Date().toISOString(),
        attachments,
      },
    ]);
    this.composerForm.controls.message.setValue('');
    this.sending.set(true);
    this.thinking.set(true);
    this.activeNodeName.set(null);
    this.activeNodeModel.set(null);
    this.streamingContent.set('');
    this.scrollToBottom();

    this.streamSub?.unsubscribe();
    this.streamSub = this.chatService
      .streamSend({
        conversationId: this.conversationId(),
        message,
        workflowId: workflowId > 0 ? workflowId : null,
        userProviderId: workflowId > 0 ? null : provider?.id ?? null,
        model: workflowId > 0 ? null : model,
        attachmentIds: attachments.map((attachment) => attachment.id),
      })
      .subscribe({
        next: (event) => this.handleStreamEvent(event, tempUserId, message, attachments),
        error: () => {
          this.sending.set(false);
          this.thinking.set(false);
          this.activeNodeName.set(null);
          this.activeNodeModel.set(null);
          this.streamingContent.set('');
          this.messages.update((list) => list.filter((m) => m.id !== tempUserId));
          this.composerForm.controls.message.setValue(message);
          this.toast.error('toast.chatError');
        },
      });
  }

  toggleHistory(): void {
    this.historyOpen.update((open) => !open);
  }

  private handleStreamEvent(
    event: ChatStreamEvent,
    tempUserId: string,
    message: string,
    attachments: AttachmentItem[],
  ): void {
    switch (event.type) {
      case 'ready': {
        if (event.conversationId) {
          this.conversationId.set(event.conversationId);
        }
        if (event.title) {
          this.conversationTitle.set(event.title);
        }
        if (event.userMessage) {
          this.messages.update((list) => [
            ...list.filter((m) => m.id !== tempUserId),
            {
              id: `u-${event.userMessage!.id}`,
              role: 'user',
              content: event.userMessage!.content,
              createdAt: event.userMessage!.createdAt,
              attachments: event.userMessage!.attachments ?? attachments,
            },
          ]);
        }
        this.scrollToBottom();
        break;
      }
      case 'node-progress':
      case 'nodeProgress': {
        const status = (event.status ?? '').toLowerCase();
        // Prefer explicit node fields; Content is a BE display label ("Name · model").
        const label = (event.content || '').trim();
        const name = (event.nodeName || event.nodeType || '').trim() || label;
        if (!status || status === 'started' || status === 'running') {
          this.thinking.set(true);
          if (name) {
            // If Content already includes model, keep name/model fields separate when possible.
            if (event.nodeName?.trim()) {
              this.activeNodeName.set(event.nodeName.trim());
              this.activeNodeModel.set(event.model?.trim() || null);
            } else if (label.includes(' · ')) {
              const [n, ...rest] = label.split(' · ');
              this.activeNodeName.set(n.trim());
              this.activeNodeModel.set(rest.join(' · ').trim() || null);
            } else {
              this.activeNodeName.set(name);
              this.activeNodeModel.set(event.model?.trim() || null);
            }
          }
          this.appRef.tick();
        } else if (status === 'failed' && event.message) {
          this.toast.error(event.message);
        }
        this.scrollToBottom();
        break;
      }
      case 'delta': {
        if (event.content) {
          // Keep node label visible; only leave thinking-pulse once tokens arrive.
          this.thinking.set(false);
          this.streamingContent.update((text) => text + event.content);
          this.scrollToBottom();
        }
        break;
      }
      case 'done': {
        this.finishStreamSuccess(event, tempUserId, attachments);
        break;
      }
      case 'error': {
        this.sending.set(false);
        this.thinking.set(false);
        this.activeNodeName.set(null);
        this.activeNodeModel.set(null);
        this.streamingContent.set('');
        this.messages.update((list) => list.filter((m) => m.id !== tempUserId));
        this.composerForm.controls.message.setValue(message);
        this.toast.error(event.message || 'toast.chatError');
        break;
      }
    }
  }

  private finishStreamSuccess(
    event: ChatStreamEvent,
    tempUserId: string,
    attachments: AttachmentItem[],
  ): void {
    this.sending.set(false);
    this.thinking.set(false);
    this.activeNodeName.set(null);
    this.activeNodeModel.set(null);
    this.streamingContent.set('');

    if (!event.response || !event.conversationId) {
      this.toast.error('toast.chatError');
      return;
    }

    this.conversationId.set(event.conversationId);
    if (event.title) {
      this.conversationTitle.set(event.title);
    }

    this.messages.update((list) => {
      const withoutTemp = list.filter((m) => m.id !== tempUserId);
      const hasUser = event.userMessage
        ? withoutTemp.some((m) => m.id === `u-${event.userMessage!.id}`)
        : true;
      const next = [...withoutTemp];
      if (event.userMessage && !hasUser) {
        next.push({
          id: `u-${event.userMessage.id}`,
          role: 'user',
          content: event.userMessage.content,
          createdAt: event.userMessage.createdAt,
          attachments: event.userMessage.attachments ?? attachments,
        });
      }
      next.push({
        id: `a-${event.response!.id}`,
        role: 'assistant',
        content: event.response!.content,
        model: event.response!.model,
        createdAt: event.response!.createdAt,
      });
      return next;
    });

    this.pendingAttachments.set([]);

    this.conversations.update((list) => {
      const existing = list.find((c) => c.id === event.conversationId);
      const item: ConversationListItem = {
        id: event.conversationId!,
        title: event.title || existing?.title || 'Chat',
        createdAt: existing?.createdAt ?? new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isPinned: existing?.isPinned ?? false,
        folderId: existing?.folderId ?? null,
      };
      return [item, ...list.filter((c) => c.id !== event.conversationId)].sort(
        (a, b) => Number(Boolean(b.isPinned)) - Number(Boolean(a.isPinned)),
      );
    });

    if (!this.route.snapshot.paramMap.get('conversationId')) {
      const workflowId = this.selectedWorkflowId();
      void this.router.navigate(['/chat', event.conversationId], {
        replaceUrl: true,
        queryParams: workflowId > 0 ? { workflowId } : {},
      });
    }

    this.scrollToBottom();
    this.focusComposer();
  }

  private tryApplyPendingWorkflow(): void {
    if (!this.pendingWorkflowId || !this.workflows().length) {
      return;
    }
    this.applyWorkflowSelection(this.pendingWorkflowId, { syncUrl: false });
  }

  private applyWorkflowSelection(
    workflowId: number,
    options: { syncUrl?: boolean } = {},
  ): void {
    if (workflowId > 0 && !this.workflows().some((w) => w.id === workflowId)) {
      return;
    }

    const syncUrl = options.syncUrl !== false && !this.syncingWorkflowFromRoute;
    this.selectedWorkflowId.set(workflowId);
    this.composerForm.controls.workflowId.setValue(workflowId, { emitEvent: false });
    const enabled = workflowId > 0;
    this.workflowMode.set(enabled);

    // Provider/Model UI is hidden in chat; always use account defaults for Default model.
    const providerCtrl = this.composerForm.controls.userProviderId;
    const modelCtrl = this.composerForm.controls.model;
    providerCtrl.disable({ emitEvent: false });
    modelCtrl.disable({ emitEvent: false });
    const provider = this.selectedProvider();
    if (provider) {
      providerCtrl.setValue(provider.id, { emitEvent: false });
      if (provider.defaultModel) {
        modelCtrl.setValue(provider.defaultModel, { emitEvent: false });
      }
    }

    if (syncUrl) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { workflowId: workflowId > 0 ? workflowId : null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    }
  }

  private loadConversation(id: number): void {
    this.loading.set(true);
    this.chatService.getConversation(id).subscribe({
      next: (detail) => {
        this.conversationId.set(detail.id);
        this.conversationTitle.set(detail.title);
        this.messages.set(
          detail.turns.flatMap((turn) => {
            const rows: ChatUiMessage[] = [
              {
                id: `u-${turn.userMessage.id}`,
                role: 'user',
                content: turn.userMessage.content,
                createdAt: turn.userMessage.createdAt,
                attachments: turn.userMessage.attachments,
              },
            ];
            for (const response of turn.responses) {
              rows.push({
                id: `a-${response.id}`,
                role: 'assistant',
                content: response.content,
                model: response.model,
                createdAt: response.createdAt,
              });
            }
            return rows;
          }),
        );
        this.loading.set(false);
        this.scrollToBottom();
        this.focusComposer();
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('toast.loadError');
        void this.router.navigate(['/chat']);
      },
    });
  }

  private scrollToBottom(): void {
    queueMicrotask(() => {
      const el = this.messageList?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }

  private replaceAndSortConversation(updated: ConversationListItem): void {
    this.conversations.update((items) =>
      items
        .map((item) => (item.id === updated.id ? updated : item))
        .sort((a, b) => Number(Boolean(b.isPinned)) - Number(Boolean(a.isPinned))),
    );
  }

  private focusComposer(): void {
    queueMicrotask(() => this.composerInput?.nativeElement.focus());
  }
}
