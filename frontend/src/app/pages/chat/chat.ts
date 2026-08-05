import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import {
  AttachmentItem,
  ChatUiMessage,
  ConversationFolder,
  ConversationListItem,
} from '../../core/models/chat.model';
import { ConnectedProvider } from '../../core/models/provider.model';
import { Workflow } from '../../core/models/workflow.model';
import { ChatService } from '../../core/services/chat.service';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { WorkflowService } from '../../core/services/workflow.service';
import { ModelSelect } from '../../shared/components/model-select/model-select';

@Component({
  selector: 'app-chat',
  imports: [ReactiveFormsModule, TranslatePipe, ModelSelect],
  templateUrl: './chat.html',
  styleUrl: './chat.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Chat implements OnInit {
  @ViewChild('messageList') private messageList?: ElementRef<HTMLElement>;
  @ViewChild('composerInput') private composerInput?: ElementRef<HTMLTextAreaElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly chatService = inject(ChatService);
  private readonly providerService = inject(ProviderService);
  private readonly workflowService = inject(WorkflowService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

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
  readonly selectedProviderId = signal(0);
  readonly workflowMode = signal(false);
  readonly uploadingAttachments = signal(0);

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

  ngOnInit(): void {
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

  providerIdValue(): number {
    return this.selectedProviderId();
  }

  onWorkflowChange(event: Event): void {
    const workflowId = Number((event.target as HTMLSelectElement).value) || 0;
    this.composerForm.controls.workflowId.setValue(workflowId);
    const enabled = workflowId > 0;
    this.workflowMode.set(enabled);

    const providerCtrl = this.composerForm.controls.userProviderId;
    const modelCtrl = this.composerForm.controls.model;
    if (enabled) {
      providerCtrl.disable({ emitEvent: false });
      modelCtrl.disable({ emitEvent: false });
    } else {
      providerCtrl.enable({ emitEvent: false });
      modelCtrl.enable({ emitEvent: false });
    }
  }

  onProviderChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
    const provider = this.providers().find((p) => p.id === id);
    this.selectedProviderId.set(id);
    this.composerForm.patchValue({
      userProviderId: id,
      model: provider?.defaultModel ?? '',
    });
  }

  newChat(): void {
    this.historyOpen.set(false);
    void this.router.navigate(['/chat']);
  }

  openConversation(id: number): void {
    this.historyOpen.set(false);
    void this.router.navigate(['/chat', id]);
  }

  conversationsForFolder(folderId: number | null): ConversationListItem[] {
    return this.conversations().filter((item) => (item.folderId ?? null) === folderId);
  }

  createFolder(): void {
    const name = prompt(this.translate.instant('chat.newFolder'))?.trim();
    if (!name) {
      return;
    }

    this.chatService.createFolder(name).subscribe({
      next: (folder) => this.folders.update((folders) => [...folders, folder]),
      error: () => this.toast.error('toast.folderError'),
    });
  }

  renameFolder(folder: ConversationFolder, event: Event): void {
    event.stopPropagation();
    const name = prompt(this.translate.instant('chat.folder'), folder.name)?.trim();
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

  deleteFolder(folder: ConversationFolder, event: Event): void {
    event.stopPropagation();
    if (!confirm(`${this.translate.instant('chat.deleteFolder')}: ${folder.name}?`)) {
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

  deleteConversation(item: ConversationListItem, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Delete "${item.title}"?`)) {
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

    const workflowId = Number(this.composerForm.controls.workflowId.value) || 0;
    const message = this.composerForm.controls.message.value.trim();
    if (!message) {
      this.composerForm.controls.message.markAsTouched();
      return;
    }

    if (workflowId <= 0) {
      if (this.composerForm.controls.userProviderId.invalid || this.composerForm.controls.model.invalid) {
        this.composerForm.markAllAsTouched();
        this.toast.error('toast.connectProviderFirst');
        return;
      }
    }

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
    this.scrollToBottom();

    this.chatService
      .send({
        conversationId: this.conversationId(),
        message,
        workflowId: workflowId > 0 ? workflowId : null,
        userProviderId: workflowId > 0 ? null : this.providerIdValue(),
        model: workflowId > 0 ? null : this.composerForm.controls.model.value,
        attachmentIds: attachments.map((attachment) => attachment.id),
      })
      .subscribe({
        next: (result) => {
          this.sending.set(false);
          this.conversationId.set(result.conversationId);
          this.conversationTitle.set(result.title);
          this.messages.update((list) => [
            ...list.filter((m) => m.id !== tempUserId),
            {
              id: `u-${result.userMessage.id}`,
              role: 'user',
              content: result.userMessage.content,
              createdAt: result.userMessage.createdAt,
              attachments: result.userMessage.attachments ?? attachments,
            },
            {
              id: `a-${result.response.id}`,
              role: 'assistant',
              content: result.response.content,
              model: result.response.model,
              createdAt: result.response.createdAt,
            },
          ]);
          this.pendingAttachments.set([]);

          this.conversations.update((list) => {
            const existing = list.find((c) => c.id === result.conversationId);
            const item: ConversationListItem = {
              id: result.conversationId,
              title: result.title,
              createdAt: existing?.createdAt ?? new Date().toISOString(),
              updatedAt: new Date().toISOString(),
              isPinned: existing?.isPinned ?? false,
              folderId: existing?.folderId ?? null,
            };
            return [item, ...list.filter((c) => c.id !== result.conversationId)].sort(
              (a, b) => Number(Boolean(b.isPinned)) - Number(Boolean(a.isPinned)),
            );
          });

          if (!this.route.snapshot.paramMap.get('conversationId')) {
            void this.router.navigate(['/chat', result.conversationId], { replaceUrl: true });
          }

          this.scrollToBottom();
          this.focusComposer();
        },
        error: () => {
          this.sending.set(false);
          this.messages.update((list) => list.filter((m) => m.id !== tempUserId));
          this.composerForm.controls.message.setValue(message);
          this.toast.error('toast.chatError');
        },
      });
  }

  toggleHistory(): void {
    this.historyOpen.update((open) => !open);
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
