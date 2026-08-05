import { ChangeDetectionStrategy, Component, HostListener, OnInit, computed, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AppPermissions } from '../../core/auth/permissions';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
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
  readonly permissions = AppPermissions;

  readonly initials = computed(() => {
    const u = this.auth.currentUser();
    return u ? `${u.firstName[0] ?? ''}${u.lastName[0] ?? ''}`.toUpperCase() : 'NX';
  });

  constructor(
    readonly auth: AuthService,
    readonly theme: ThemeService,
  ) {}

  ngOnInit(): void {
    if (!this.auth.currentUser()) {
      this.auth.loadCurrentUser().subscribe({ error: () => this.auth.logout() });
    }
  }

  can(permission: string): boolean {
    return this.auth.can(permission);
  }

  toggleTheme(event: Event): void {
    event.stopPropagation();
    this.theme.toggle();
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
