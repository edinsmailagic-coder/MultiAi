import { ChangeDetectionStrategy, Component, DestroyRef, HostListener, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter } from 'rxjs';
import { Brand } from '../brand/brand';

export interface TopBarNavItem {
  labelKey: string;
  href?: string;
  children?: { labelKey: string; href: string }[];
}

@Component({
  selector: 'app-top-bar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, Brand],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopBar {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** When true, renders authenticated app navigation instead of marketing nav. */
  readonly appMode = input(false);
  readonly showSubBar = input(true);

  readonly mobileOpen = signal(false);
  readonly openDropdown = signal<string | null>(null);

  readonly marketingNav: TopBarNavItem[] = [
    {
      labelKey: 'landing.nav.meet',
      children: [
        { labelKey: 'landing.nav.meetClaude', href: '/login' },
        { labelKey: 'landing.nav.meetTeam', href: '/login' },
      ],
    },
    {
      labelKey: 'landing.nav.platform',
      children: [
        { labelKey: 'landing.nav.api', href: '/login' },
        { labelKey: 'landing.nav.workflows', href: '/login' },
      ],
    },
    {
      labelKey: 'landing.nav.solutions',
      children: [
        { labelKey: 'landing.nav.enterprise', href: '/login' },
        { labelKey: 'landing.nav.startup', href: '/login' },
      ],
    },
    { labelKey: 'landing.nav.pricing', href: '/login' },
    {
      labelKey: 'landing.nav.resources',
      children: [
        { labelKey: 'landing.nav.docs', href: '/login' },
        { labelKey: 'landing.nav.blog', href: '/login' },
      ],
    },
  ];

  readonly appNav: TopBarNavItem[] = [
    { labelKey: 'nav.dashboard', href: '/dashboard' },
    { labelKey: 'nav.chat', href: '/chat' },
    { labelKey: 'nav.workflows', href: '/workflows' },
  ];

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.closeMenus());
  }

  get navItems(): TopBarNavItem[] {
    return this.appMode() ? this.appNav : this.marketingNav;
  }

  toggleMobile(): void {
    this.mobileOpen.update((open) => !open);
    if (!this.mobileOpen()) {
      this.openDropdown.set(null);
    }
  }

  toggleDropdown(key: string, event: Event): void {
    event.preventDefault();
    this.openDropdown.update((current) => (current === key ? null : key));
  }

  closeMenus(): void {
    this.mobileOpen.set(false);
    this.openDropdown.set(null);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenus();
  }
}
