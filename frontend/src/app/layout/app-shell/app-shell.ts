import { ChangeDetectionStrategy, Component, HostListener, OnInit, computed, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { TopBar } from '../../shared/components/top-bar/top-bar';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, TranslatePipe, TopBar],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell implements OnInit {
  readonly menuOpen = signal(false);

  readonly initials = computed(() => {
    const u = this.auth.currentUser();
    return u ? `${u.firstName[0] ?? ''}${u.lastName[0] ?? ''}`.toUpperCase() : 'NX';
  });

  constructor(readonly auth: AuthService) {}

  ngOnInit(): void {
    if (!this.auth.currentUser()) {
      this.auth.loadCurrentUser().subscribe({ error: () => this.auth.logout() });
    }
  }

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.closeMenu();
    this.auth.logout();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }
}
